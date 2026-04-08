using System.Net;
using System.Text;
using System.Text.Json;
using Aiursoft.Translate.Configuration;
using Aiursoft.Translate.Services;

namespace Aiursoft.Translate.Tests.IntegrationTests;

[TestClass]
public class TranslateRateLimitTests : TestBase
{
    /// <summary>
    /// Set both per-hour limits to 1 via GlobalSettings so the second request always hits the ceiling.
    /// </summary>
    private async Task SetRateLimitsToOne()
    {
        using var scope = Server!.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
        await settings.UpdateSettingAsync(SettingsMap.GuestTranslateMaxPerHour, "1");
        await settings.UpdateSettingAsync(SettingsMap.UserTranslateMaxPerHour, "1");
    }

    /// <summary>
    /// POST to /Translate/TranslateStream with a JSON body, forwarding the CSRF token as a request header.
    /// The response status code (401 / 429 / 200) is what matters; the translation result itself is irrelevant.
    /// </summary>
    private async Task<HttpResponseMessage> PostTranslateStreamAsync()
    {
        var token = await GetAntiCsrfToken("/");
        var json = JsonSerializer.Serialize(new { content = "Hello World", targetLanguage = "zh-CN" });
        var request = new HttpRequestMessage(HttpMethod.Post, "/Translate/TranslateStream")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("RequestVerificationToken", token);
        return await Http.SendAsync(request);
    }

    // -------------------------------------------------------------------------
    // Guest (anonymous) rate-limit tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task GuestRateLimit_FirstRequest_IsAllowed()
    {
        await SetRateLimitsToOne();

        // The limit is 1, so the very first anonymous request must NOT be rejected with 401.
        var response = await PostTranslateStreamAsync();

        Assert.AreNotEqual(
            HttpStatusCode.Unauthorized,
            response.StatusCode,
            "First guest request should be allowed through the rate limiter.");
    }

    [TestMethod]
    public async Task GuestRateLimit_SecondRequest_IsBlocked()
    {
        await SetRateLimitsToOne();

        // Exhaust the quota with the first request.
        await PostTranslateStreamAsync();

        // The second request must be rejected with 401 + requireLogin payload.
        var response = await PostTranslateStreamAsync();
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "Guest should receive 401 once the per-hour quota is exceeded.");

        var body = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(body.Contains("requireLogin"),
            "Response body should contain 'requireLogin' to signal the front-end to open the login page.");
    }

    // -------------------------------------------------------------------------
    // Authenticated user rate-limit tests
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task UserRateLimit_FirstRequest_IsAllowed()
    {
        await SetRateLimitsToOne();
        await LoginAsAdmin();

        // The limit is 1, so the very first authenticated request must NOT be rejected with 429.
        var response = await PostTranslateStreamAsync();

        Assert.AreNotEqual(
            HttpStatusCode.TooManyRequests,
            response.StatusCode,
            "First authenticated request should be allowed through the rate limiter.");
    }

    [TestMethod]
    public async Task UserRateLimit_SecondRequest_IsBlocked()
    {
        await SetRateLimitsToOne();
        await LoginAsAdmin();

        // Exhaust the quota with the first request.
        await PostTranslateStreamAsync();

        // The second request must be rejected with 429 + tooManyRequests payload.
        var response = await PostTranslateStreamAsync();
        Assert.AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode,
            "Authenticated user should receive 429 once the per-hour quota is exceeded.");

        var body = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(body.Contains("tooManyRequests"),
            "Response body should contain 'tooManyRequests' to signal the front-end to display the throttle message.");
    }
}
