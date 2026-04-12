using Aiursoft.Translate.Configuration;
using Aiursoft.Translate.Models.TranslateViewModels;
using Aiursoft.Translate.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.Translate.Controllers;

[LimitPerMin]
public class TranslateController(
    TranslationCacheService translator, 
    GuestTranslateRateLimiter rateLimiter,
    GlobalSettingsService globalSettingsService) : Controller
{
    [Route("")]
    [Route("Translate")]
    [Route("Translate/Index")]
    [RenderInNavBar(
        NavGroupName = "Tools",
        NavGroupOrder = 8000,
        CascadedLinksGroupName = "Tools",
        CascadedLinksIcon = "languages",
        CascadedLinksOrder = 1,
        LinkText = "Translate",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var model = new IndexViewModel
        {
            ProjectSlogan = await globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectSlogan)
        };
        return this.StackView(model);
    }

    [RenderInNavBar(
        NavGroupName = "About",
        NavGroupOrder = 9000,
        CascadedLinksGroupName = "About",
        CascadedLinksIcon = "server",
        CascadedLinksOrder = 1,
        LinkText = "Self Host",
        LinkOrder = 1)]
    public async Task<IActionResult> SelfHost()
    {
        var model = new Models.HomeViewModels.SelfHostViewModel
        {
            ProjectSlogan = await globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectSlogan)
        };
        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (User.Identity!.IsAuthenticated)
        {
            var userId = User.Identity.Name ?? "unknown";
            if (!await rateLimiter.TryConsumeAsUserAsync(userId))
            {
                return StatusCode(429, new { tooManyRequests = true });
            }
        }
        else
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!await rateLimiter.TryConsumeAsGuestAsync(ip))
            {
                return StatusCode(401, new { requireLogin = true });
            }
        }

        try
        {
            var translated = await translator.GetOrTranslateAsync(request.Content, request.TargetLanguage);
            return Json(new TranslateResponse { TranslatedContent = translated });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task TranslateStream([FromBody] TranslateRequest request)
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = 400;
            return;
        }

        if (User.Identity!.IsAuthenticated)
        {
            var userId = User.Identity.Name ?? "unknown";
            if (!await rateLimiter.TryConsumeAsUserAsync(userId))
            {
                Response.StatusCode = 429;
                Response.ContentType = "application/json";
                await Response.WriteAsync("{\"tooManyRequests\":true}");
                return;
            }
        }
        else
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (!await rateLimiter.TryConsumeAsGuestAsync(ip))
            {
                Response.StatusCode = 401;
                Response.ContentType = "application/json";
                await Response.WriteAsync("{\"requireLogin\":true}");
                return;
            }
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        try
        {
            await foreach (var part in translator.GetOrTranslateStreamAsync(request.Content, request.TargetLanguage, HttpContext.RequestAborted))
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
