using Aiursoft.Translate.Configuration;
using Microsoft.AspNetCore.Mvc;
using Aiursoft.Translate.Services;
using Aiursoft.Translate.Services.FileStorage;

namespace Aiursoft.Translate.Views.Shared.Components.MarketingFooter;

public class MarketingFooter(
    GlobalSettingsService globalSettingsService,
    StorageService storageService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(MarketingFooterViewModel? model = null)
    {
        model ??= new MarketingFooterViewModel();
        model.BrandName = await globalSettingsService.GetSettingValueAsync(SettingsMap.BrandName);
        model.BrandHomeUrl = await globalSettingsService.GetSettingValueAsync(SettingsMap.BrandHomeUrl);
        model.Icp = await globalSettingsService.GetSettingValueAsync(SettingsMap.Icp);
        model.ProjectName = await globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectName);
        model.ProjectSlogan = await globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectSlogan);
        var logoPath = await globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectLogo);

        if (!string.IsNullOrWhiteSpace(logoPath))
        {
            model.LogoUrl = storageService.RelativePathToInternetUrl(logoPath, HttpContext);
        }
        
        return View(model);
    }
}
