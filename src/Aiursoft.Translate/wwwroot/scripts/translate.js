(function () {
    const { basicSetup, EditorView, EditorState, markdown, oneDark } = window.CM;
    const config = document.getElementById('loc-data');

    function getLocalizedText(key, defaultText) {
        const el = document.querySelector(`#loc-data span[data-key="${key}"]`);
        return el ? el.innerText : defaultText;
    }

    function setEditorValue(editor, text) {
        editor.dispatch({
            changes: { from: 0, to: editor.state.doc.length, insert: text }
        });
    }

    function scrollEditorToBottom(editor) {
        const scroll = editor.scrollDOM;
        const nearBottom = scroll.scrollTop + scroll.clientHeight >= scroll.scrollHeight - 50;
        if (nearBottom) {
            editor.dispatch({
                effects: EditorView.scrollIntoView(editor.state.doc.length)
            });
        }
    }

    function updateTargetLanguage(content) {
        const trimmed = (content || '').trim();
        if (!trimmed) return;
        const isEnglish = /^[#\s*]*[A-Za-z]/.test(trimmed);
        const targetSelect = document.getElementById('targetLanguage');
        const desired = isEnglish ? 'zh-CN' : 'en-US';
        if (targetSelect.value !== desired) targetSelect.value = desired;
    }

    // --- View Mode Management ---
    const contentArea = document.querySelector('.content-area');
    const viewBtns = document.querySelectorAll('.view-btn');
    let currentView = 'both';
    const isMobile = () => window.matchMedia('(max-width: 991.98px)').matches;

    function setViewMode(view) {
        // On mobile, 'both' behaves like 'source'
        if (isMobile() && view === 'both') {
            view = 'source';
        }
        currentView = view;
        contentArea.setAttribute('data-view', view);
        viewBtns.forEach(btn => {
            btn.classList.toggle('active', btn.getAttribute('data-view') === view);
        });

        // Refresh CodeMirror editors to ensure proper rendering after visibility change
        requestAnimationFrame(() => {
            sourceEditor.requestMeasure();
            resultEditor.requestMeasure();
        });
    }

    viewBtns.forEach(btn => {
        btn.addEventListener('click', function () {
            setViewMode(this.getAttribute('data-view'));
        });
    });

    // Initialize: reset all buttons first, then activate the correct one
    viewBtns.forEach(b => b.classList.remove('active'));
    if (isMobile()) {
        setViewMode('source');
        document.querySelector('.view-btn[data-view="source"].d-lg-none')?.classList.add('active');
    } else {
        setViewMode('both');
        document.querySelector('.view-btn[data-view="both"]')?.classList.add('active');
    }

    // Handle resize: switch between mobile/desktop modes
    let resizeTimeout;
    window.addEventListener('resize', () => {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(() => {
            // If currently on 'both' and switched to mobile, go to 'source'
            if (isMobile() && currentView === 'both') {
                setViewMode('source');
            }
            // Refresh editors
            sourceEditor.requestMeasure();
            resultEditor.requestMeasure();
        }, 150);
    });

    const isDarkMode = config.dataset.darkMode === 'true';
    const commonExtensions = [
    basicSetup,
    markdown(),
    EditorView.lineWrapping,
    ...(isDarkMode ? [oneDark] : [])
    ];

    let languageAutoDetected = false;
    let detectTimeout;
    let targetLanguageSelected = false;
    document.getElementById('targetLanguage').addEventListener('change', () => {
        targetLanguageSelected = true;
        clearTimeout(detectTimeout);
    });

    const sourceEditor = new EditorView({
        doc: '# Hello World\n\nPaste your markdown here to translate!',
        extensions: [
        ...commonExtensions,
        EditorView.updateListener.of(update => {
            if (!update.docChanged || languageAutoDetected || targetLanguageSelected) return;
            clearTimeout(detectTimeout);
            detectTimeout = setTimeout(() => {
                if (targetLanguageSelected) return;
                updateTargetLanguage(sourceEditor.state.doc.toString());
                languageAutoDetected = true;
            }, 1000);
        })
        ],
        parent: document.getElementById('source-editor-container')
    });

    const resultEditor = new EditorView({
        extensions: [
        ...commonExtensions,
        EditorState.readOnly.of(true)
        ],
        parent: document.getElementById('result-editor-container')
    });

    let abortController = null;
    const btn = document.getElementById('translateBtn');

    btn.addEventListener('click', function () {
        if (abortController) {
            abortController.abort();
            return;
        }

        const content = sourceEditor.state.doc.toString();
        const targetLanguage = document.getElementById('targetLanguage').value;

        if (!content) {
            alert(getLocalizedText('enter-content', 'Please enter some content to translate.'));
            return;
        }

        // On mobile: auto-switch to result view when translating
        if (isMobile()) {
            setViewMode('result');
        }

        document.getElementById('btnTextNormal').classList.add('d-none');
        document.getElementById('btnTextStop').classList.remove('d-none');
        document.getElementById('btnLoading').classList.remove('d-none');
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-danger');

        setEditorValue(resultEditor, '');
        let translatedFullContent = '';
        abortController = new AbortController();

        fetch(config.dataset.translateUrl, {
            method: 'POST',
            signal: abortController.signal,
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ content, targetLanguage })
        }).then(async response => {
            if (response.status === 401) {
                const data = await response.json().catch(() => ({}));
                if (data.requireLogin) {
                    window.open(config.dataset.loginUrl, '_blank');
                    return;
                }
            }
            if (response.status === 429) {
                alert(getLocalizedText('too-many-requests', '使用过于频繁，请稍等。'));
                return;
            }
            if (!response.ok) {
                const errorText = await response.text();
                alert(getLocalizedText('translation-failed-prefix', 'Translation failed: ') + errorText);
                setEditorValue(resultEditor, getLocalizedText('translation-failed', 'Translation failed.'));
                return;
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                translatedFullContent += decoder.decode(value, { stream: true });
                setEditorValue(resultEditor, translatedFullContent);
                scrollEditorToBottom(resultEditor);
            }
        }).catch(error => {
            if (error.name !== 'AbortError') {
                alert(getLocalizedText('translation-failed-prefix', 'Translation failed: ') + error);
                setEditorValue(resultEditor, getLocalizedText('translation-failed', 'Translation failed.'));
            }
        }).finally(() => {
            abortController = null;
            document.getElementById('btnTextNormal').classList.remove('d-none');
            document.getElementById('btnTextStop').classList.add('d-none');
            document.getElementById('btnLoading').classList.add('d-none');
            btn.classList.remove('btn-danger');
            btn.classList.add('btn-primary');
        });
    });
})();
