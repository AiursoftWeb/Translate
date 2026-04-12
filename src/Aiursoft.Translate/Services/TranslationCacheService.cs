using Aiursoft.Translate.Entities;
using Aiursoft.Dotlang.Shared;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

using System.Runtime.CompilerServices;

namespace Aiursoft.Translate.Services;

public class TranslationCacheService(TranslateDbContext dbContext, OllamaBasedTranslatorEngine translator)
{
    private string GetHash(string content, string targetLanguage)
    {
        var input = $"{content}|{targetLanguage}";
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    public async Task<string> GetOrTranslateAsync(string content, string targetLanguage)
    {
        var hash = GetHash(content, targetLanguage);
        var cached = await dbContext.TranslationCaches
            .FirstOrDefaultAsync(c => c.SourceHash == hash);

        if (cached != null)
        {
            return cached.TranslatedContent;
        }

        var translated = await translator.TranslateAsync(content, targetLanguage);

        dbContext.TranslationCaches.Add(new TranslationCache
        {
            SourceHash = hash,
            SourceContent = content,
            TargetLanguage = targetLanguage,
            TranslatedContent = translated
        });
        await dbContext.SaveChangesAsync();

        return translated;
    }

    public async IAsyncEnumerable<string> GetOrTranslateStreamAsync(
        string content, 
        string targetLanguage, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var hash = GetHash(content, targetLanguage);
        var cached = await dbContext.TranslationCaches
            .FirstOrDefaultAsync(c => c.SourceHash == hash, cancellationToken);

        if (cached != null)
        {
            yield return cached.TranslatedContent;
            yield break;
        }

        var fullContent = new StringBuilder();
        await foreach (var part in translator.TranslateStreamAsync(content, targetLanguage, cancellationToken))
        {
            fullContent.Append(part);
            yield return part;
        }

        dbContext.TranslationCaches.Add(new TranslationCache
        {
            SourceHash = hash,
            SourceContent = content,
            TargetLanguage = targetLanguage,
            TranslatedContent = fullContent.ToString()
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
