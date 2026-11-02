using Microsoft.Playwright;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class CookieConsentTests : IClassFixture<TestServerFixture>
{
    private const string StorageKey = "cookieConsentChoice";

    private readonly TestServerFixture _server;

    public CookieConsentTests(TestServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task FirstVisit_ShowsEqualChoicesAndLoadsNoThirdPartyTags()
    {
        var (context, page, thirdPartyRequests) = await OpenSiteAsync();
        await using var contextScope = context;

        await Assertions.Expect(page.Locator("#consentDiv")).ToBeVisibleAsync();
        Assert.Empty(thirdPartyRequests);
        Assert.Equal(0, await page.EvaluateAsync<int>("() => document.querySelectorAll('iframe[src*=\"googletagmanager\"]').length"));

        var accept = await ButtonAppearanceAsync(page, "#consentAcceptButton");
        var decline = await ButtonAppearanceAsync(page, "#consentDeclineButton");
        Assert.Equal(accept, decline);
    }

    [Fact]
    public async Task Accept_LoadsTagManagerAndIsRemembered()
    {
        var (context, page, thirdPartyRequests) = await OpenSiteAsync();
        await using var contextScope = context;

        await page.RunAndWaitForResponseAsync(() => page.ClickAsync("#consentAcceptButton"), IsTagManagerResponse);
        await page.WaitForSelectorAsync("#consentDiv", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden });
        Assert.Contains(thirdPartyRequests, url => url.Contains("googletagmanager.com/gtm.js?id="));
        Assert.Equal("accepted", await ReadStoredChoiceAsync(page));

        thirdPartyRequests.Clear();
        await page.RunAndWaitForResponseAsync(() => page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load }), IsTagManagerResponse);
        await Assertions.Expect(page.Locator("#consentDiv")).ToBeHiddenAsync();
        Assert.Contains(thirdPartyRequests, url => url.Contains("googletagmanager.com/gtm.js?id="));
    }

    [Fact]
    public async Task Decline_LoadsNoTagsAndIsRemembered()
    {
        var (context, page, thirdPartyRequests) = await OpenSiteAsync();
        await using var contextScope = context;

        await page.ClickAsync("#consentDeclineButton");
        await page.WaitForSelectorAsync("#consentDiv", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden });
        Assert.Equal("declined", await ReadStoredChoiceAsync(page));

        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load });
        await Assertions.Expect(page.Locator("#consentDiv")).ToBeHiddenAsync();
        Assert.Empty(thirdPartyRequests);
    }

    [Fact]
    public async Task CookieSettings_WithdrawingConsentStopsTagsAndClearsCookies()
    {
        var (context, page, thirdPartyRequests) = await OpenSiteAsync();
        await using var contextScope = context;

        await page.ClickAsync("#consentAcceptButton");
        await page.EvaluateAsync("() => { document.cookie = '_ga=GA1.1.1; path=/'; document.cookie = '_gcl_au=1.1.1; path=/'; document.cookie = 'unrelated=1; path=/'; }");

        await page.ClickAsync("#cookieSettingsButton");
        await Assertions.Expect(page.Locator("#consentDiv")).ToBeVisibleAsync();
        Assert.Equal("consentAcceptButton", await page.EvaluateAsync<string>("() => document.activeElement.id"));

        thirdPartyRequests.Clear();
        await page.EvaluateAsync("() => { window.beforeReload = true; }");
        await page.ClickAsync("#consentDeclineButton");
        await page.WaitForFunctionAsync("() => !window.beforeReload && document.readyState === 'complete'");

        Assert.Empty(thirdPartyRequests);
        await Assertions.Expect(page.Locator("#consentDiv")).ToBeHiddenAsync();
        Assert.Equal("declined", await ReadStoredChoiceAsync(page));
        Assert.Equal("unrelated=1", await page.EvaluateAsync<string>("() => document.cookie"));

        await page.ClickAsync("#cookieSettingsButton");
        await page.RunAndWaitForResponseAsync(() => page.ClickAsync("#consentAcceptButton"), IsTagManagerResponse);
        Assert.Contains(thirdPartyRequests, url => url.Contains("googletagmanager.com/gtm.js?id="));
        Assert.Equal("accepted", await ReadStoredChoiceAsync(page));
    }

    [Fact]
    public async Task FormerImpliedConsent_IsNotHonored()
    {
        await using var context = await _server.Browser.NewContextAsync();
        await context.AddInitScriptAsync("if (!sessionStorage.getItem('seeded')) { sessionStorage.setItem('seeded', '1'); localStorage.setItem('consentOKClicked', 'true'); }");
        var (page, thirdPartyRequests) = await OpenPageAsync(context);

        await Assertions.Expect(page.Locator("#consentDiv")).ToBeVisibleAsync();
        Assert.Empty(thirdPartyRequests);
        Assert.Null(await page.EvaluateAsync<string?>("() => localStorage.getItem('consentOKClicked')"));
    }

    private async Task<(IBrowserContext Context, IPage Page, List<string> ThirdPartyRequests)> OpenSiteAsync()
    {
        var context = await _server.Browser.NewContextAsync();
        var (page, thirdPartyRequests) = await OpenPageAsync(context);
        return (context, page, thirdPartyRequests);
    }

    // Tag and player hosts are stubbed so that tests never reach Google
    private async Task<(IPage Page, List<string> ThirdPartyRequests)> OpenPageAsync(IBrowserContext context)
    {
        var thirdPartyRequests = new List<string>();
        var page = await context.NewPageAsync();
        foreach (var pattern in new[] { "https://www.googletagmanager.com/**", "https://www.youtube.com/**" })
        {
            await page.RouteAsync(pattern, async route =>
            {
                thirdPartyRequests.Add(route.Request.Url);
                await route.FulfillAsync(new RouteFulfillOptions { Status = 200, ContentType = "text/javascript", Body = "" });
            });
        }

        await page.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        return (page, thirdPartyRequests);
    }

    // A stubbed response arrives only after its route handler has recorded the request
    private static bool IsTagManagerResponse(IResponse response)
    {
        return response.Url.Contains("googletagmanager.com/gtm.js?id=");
    }

    private static Task<string?> ReadStoredChoiceAsync(IPage page)
    {
        return page.EvaluateAsync<string?>("key => localStorage.getItem(key)", StorageKey);
    }

    private static Task<string> ButtonAppearanceAsync(IPage page, string selector)
    {
        return page.EvaluateAsync<string>(@"selector => {
            const button = document.querySelector(selector);
            const style = getComputedStyle(button);
            const rect = button.getBoundingClientRect();
            return [Math.round(rect.width), Math.round(rect.height), style.color, style.backgroundColor, style.backgroundImage, style.fontSize, style.fontWeight, style.borderTopColor].join('|');
        }", selector);
    }
}
