using Aiursoft.UiStack.Layout;

namespace Aiursoft.Translate.Models.HomeViewModels;

public class SelfHostViewModel : UiStackLayoutViewModel
{
    public string? ProjectSlogan { get; set; }

    public SelfHostViewModel()
    {
        PageTitle = "Self Host";
    }
}
