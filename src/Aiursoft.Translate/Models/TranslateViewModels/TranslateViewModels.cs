using System.ComponentModel.DataAnnotations;

namespace Aiursoft.Translate.Models.TranslateViewModels;

public class TranslateRequest
{
    [Required]
    public required string Content { get; set; }

    [Required]
    public required string TargetLanguage { get; set; }
}

public class TranslateResponse
{
    public required string TranslatedContent { get; set; }
}
