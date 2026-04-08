using Aiursoft.Translate.Models.TranslateViewModels;
using Aiursoft.Translate.Services;
using Aiursoft.Dotlang.Shared;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.Translate.Controllers;

[Authorize]
[LimitPerMin]
public class TranslateController(OllamaBasedTranslatorEngine translator) : Controller
{
    [Route("")]
    [Route("Translate")]
    [Route("Translate/Index")]
    [AllowAnonymous]
    [RenderInNavBar(
        NavGroupName = "Tools",
        NavGroupOrder = 8000,
        CascadedLinksGroupName = "Tools",
        CascadedLinksIcon = "languages",
        CascadedLinksOrder = 1,
        LinkText = "Translate",
        LinkOrder = 1)]
    public IActionResult Index()
    {
        return this.StackView(new IndexViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
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

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task TranslateStream([FromBody] TranslateRequest request)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            await foreach (var part in translator.TranslateStreamAsync(request.Content, request.TargetLanguage, HttpContext.RequestAborted))
            {
                await Response.WriteAsync(part, HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }
        }
        catch (Exception ex)
        {
            // Response has already started, cannot set status code.
            await Response.WriteAsync("\n[ERROR]: " + ex.Message);
        }
    }
}
