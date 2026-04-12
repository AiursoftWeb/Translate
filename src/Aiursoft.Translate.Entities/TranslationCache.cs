using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Aiursoft.Translate.Entities;

[ExcludeFromCodeCoverage]
public class TranslationCache
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(128)]
    public required string SourceHash { get; set; }

    [Required]
    public required string SourceContent { get; set; }

    [Required]
    [MaxLength(64)]
    public required string TargetLanguage { get; set; }

    [Required]
    public required string TranslatedContent { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
}
