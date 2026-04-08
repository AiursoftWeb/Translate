// Entry point for the CodeMirror bundle.
// esbuild compiles this into codemirror-bundle.js as an IIFE exposed as window.CM.
export { basicSetup, EditorView } from 'codemirror';
export { EditorState } from '@codemirror/state';
export { markdown } from '@codemirror/lang-markdown';
export { oneDark } from '@codemirror/theme-one-dark';
