namespace Aiursoft.Translate.Models;

public enum ChunkType
{
    Translatable,
    Static
}

public class MarkdownChunk
{
    public string Content { get; set; } = string.Empty;
    public ChunkType Type { get; set; }

    public override string ToString()
    {
        return $"[{Type}]: {Content.Replace("\n", "\\n")}";
    }
}
