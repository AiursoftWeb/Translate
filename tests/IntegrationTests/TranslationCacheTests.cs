using System.Runtime.CompilerServices;
using System.Text;
using Aiursoft.Canon;
using Aiursoft.Dotlang.Shared;
using Aiursoft.GptClient.Abstractions;
using Aiursoft.GptClient.Services;
using Aiursoft.Translate.Entities;
using Aiursoft.Translate.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aiursoft.Translate.Tests.IntegrationTests;

[TestClass]
public class TranslationCacheTests : TestBase
{
    [TestMethod]
    public async Task TestCaching()
    {
        var dbContext = GetService<TranslateDbContext>();
        using var httpClient = new HttpClient();
        var chatClient = new RecordingChatClient(httpClient);
        var cacheService = CreateCacheService(dbContext, chatClient);

        const string content = "Hello World";
        const string targetLanguage = "zh-CN";

        var firstResult = await cacheService.GetOrTranslateAsync(content, targetLanguage);
        Assert.AreEqual("Translated content", firstResult);
        Assert.AreEqual(1, chatClient.RequestCount);

        var cached = await dbContext.TranslationCaches.SingleAsync(c => c.SourceContent == content);
        Assert.AreEqual(targetLanguage, cached.TargetLanguage);
        Assert.AreEqual(firstResult, cached.TranslatedContent);

        var secondResult = await cacheService.GetOrTranslateAsync(content, targetLanguage);
        Assert.AreEqual(firstResult, secondResult);
        Assert.AreEqual(1, chatClient.RequestCount, "A cache hit must not call the translation model again.");

        var count = await dbContext.TranslationCaches.CountAsync(c => c.SourceContent == content);
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public async Task TestStreamingCaching()
    {
        var dbContext = GetService<TranslateDbContext>();
        using var httpClient = new HttpClient();
        var chatClient = new RecordingChatClient(httpClient);
        var cacheService = CreateCacheService(dbContext, chatClient);

        const string content = "Streaming Hello";
        const string targetLanguage = "zh-CN";

        var firstResultBuilder = new StringBuilder();
        await foreach (var part in cacheService.GetOrTranslateStreamAsync(content, targetLanguage))
        {
            firstResultBuilder.Append(part);
        }
        var firstResult = firstResultBuilder.ToString();
        Assert.AreEqual("Translated content", firstResult);
        Assert.AreEqual(1, chatClient.RequestCount);

        var cached = await dbContext.TranslationCaches.SingleAsync(c => c.SourceContent == content);
        Assert.AreEqual(targetLanguage, cached.TargetLanguage);
        Assert.AreEqual(firstResult, cached.TranslatedContent);

        var secondResultBuilder = new StringBuilder();
        await foreach (var part in cacheService.GetOrTranslateStreamAsync(content, targetLanguage))
        {
            secondResultBuilder.Append(part);
        }
        Assert.AreEqual(firstResult, secondResultBuilder.ToString());
        Assert.AreEqual(1, chatClient.RequestCount, "A streaming cache hit must not call the translation model again.");
        Assert.AreEqual(1, await dbContext.TranslationCaches.CountAsync(c => c.SourceContent == content));
    }

    private static TranslationCacheService CreateCacheService(TranslateDbContext dbContext, ChatClient chatClient)
    {
        var translator = new OllamaBasedTranslatorEngine(
            Options.Create(new TranslateOptions { OllamaInstance = "http://localhost", OllamaModel = "test", OllamaToken = "test-token" }),
            new RetryEngine(NullLogger<RetryEngine>.Instance),
            NullLogger<OllamaBasedTranslatorEngine>.Instance,
            chatClient,
            new MarkdownShredder());
        return new TranslationCacheService(dbContext, translator);
    }

    private sealed class RecordingChatClient(HttpClient httpClient)
        : ChatClient(httpClient, NullLogger<ChatClient>.Instance)
    {
        public int RequestCount { get; private set; }

        public override Task<CompletionData> AskModel(OpenAiRequestModel model,
            string completionApiUrl, string? token, CancellationToken cancellationToken)
        {
            RequestCount++;
            var completion = new CompletionData();
            completion.SetContent("Translated content");
            return Task.FromResult(completion);
        }

        public override async IAsyncEnumerable<string> AskModelStream(OpenAiRequestModel model,
            string completionApiUrl, string? token,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            RequestCount++;
            yield return "```markdown\nTranslated ";
            await Task.Yield();
            yield return "content\n```";
        }
    }
}
