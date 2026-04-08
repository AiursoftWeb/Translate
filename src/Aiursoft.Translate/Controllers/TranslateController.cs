using Aiursoft.Translate.Authorization;
using Aiursoft.Translate.Models.TranslateViewModels;
using Aiursoft.Translate.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.Translate.Controllers;

[Authorize]
[LimitPerMin]
public class TranslateController(OllamaBasedTranslatorEngine translator) : Controller
{
    [Authorize(Policy = AppPermissionNames.CanTranslate)]
    [RenderInNavBar(
        NavGroupName = "Tools",
        NavGroupOrder = 8000,
        CascadedLinksGroupName = "Tools",
        CascadedLinksIcon = "translate",
        CascadedLinksOrder = 1,
        LinkText = "Translate",
        LinkOrder = 1)]
    public IActionResult Index()
    {
        return this.StackView(new IndexViewModel());
    }

    [HttpPost]
    [Authorize(Policy = AppPermissionNames.CanTranslate)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var translated = await translator.TranslateAsync(request.Content, request.TargetLanguage);
            return Json(new TranslateResponse { TranslatedContent = translated });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
