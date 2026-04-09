using Aiursoft.UiStack.Layout;

namespace Aiursoft.Translate.Models.TranslateViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public string? ProjectSlogan { get; set; }

    public IndexViewModel()
    {
        PageTitle = "Translate";
        ContentNoPadding = true;
    }
}
