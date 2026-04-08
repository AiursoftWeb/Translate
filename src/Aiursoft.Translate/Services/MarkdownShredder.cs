using System.Text;
using System.Text.RegularExpressions;
using Aiursoft.Translate.Models;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.Translate.Services;

public class MarkdownShredder : IScopedDependency
{
    public List<MarkdownChunk> Shred(string content, int maxLength = 1000)
    {
        var result = new List<MarkdownChunk>();
        if (string.IsNullOrEmpty(content))
        {
            return result;
        }

        // 1. Split by code blocks
        var codeBlockRegex = new Regex(@"((?<=^|\n)[ \t]*(?:`{3,}|~{3,})[^\n]*\n.*?\n[ \t]*(?:`{3,}|~{3,})(?=\n|$))", RegexOptions.Singleline);
        var parts = codeBlockRegex.Split(content);
        var matches = codeBlockRegex.Matches(content);

        int matchIndex = 0;
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (string.IsNullOrEmpty(part))
            {
                if (i % 2 != 0) 
                {
                    result.Add(new MarkdownChunk { Content = matches[matchIndex++].Value, Type = ChunkType.Static });
                }
                continue;
            }

            if (i % 2 == 0)
            {
                result.AddRange(ShredNonCodePart(part, maxLength));
            }
            else
            {
                result.Add(new MarkdownChunk { Content = matches[matchIndex++].Value, Type = ChunkType.Static });
            }
        }

        return result;
    }

    private List<MarkdownChunk> ShredNonCodePart(string content, int maxLength)
    {
        var result = new List<MarkdownChunk>();
        
        var paragraphSeparatorRegex = new Regex(@"(\n\s*\n|\r\n\s*\r\n)");
        var parts = paragraphSeparatorRegex.Split(content);
        var matches = paragraphSeparatorRegex.Matches(content);

        var currentTranslatable = new StringBuilder();
        int matchIndex = 0;

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            
            if (i % 2 == 0)
            {
                if (string.IsNullOrEmpty(part)) continue;

                if (currentTranslatable.Length + part.Length > maxLength && currentTranslatable.Length > 0)
                {
                    result.Add(new MarkdownChunk { Content = currentTranslatable.ToString(), Type = ChunkType.Translatable });
                    currentTranslatable.Clear();
                }

                if (part.Length > maxLength)
                {
                    result.Add(new MarkdownChunk { Content = part, Type = ChunkType.Translatable });
                }
                else
                {
                    currentTranslatable.Append(part);
                }
            }
            else
            {
                var separator = matches[matchIndex++].Value;
                if (currentTranslatable.Length + separator.Length > maxLength && currentTranslatable.Length > 0)
                {
                    result.Add(new MarkdownChunk { Content = currentTranslatable.ToString(), Type = ChunkType.Translatable });
                    currentTranslatable.Clear();
                }
                currentTranslatable.Append(separator);
            }
        }

        if (currentTranslatable.Length > 0)
        {
            result.Add(new MarkdownChunk { Content = currentTranslatable.ToString(), Type = ChunkType.Translatable });
        }

        return result;
    }
}
