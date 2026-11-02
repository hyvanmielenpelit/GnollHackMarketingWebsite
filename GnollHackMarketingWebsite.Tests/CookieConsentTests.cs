using Microsoft.Playwright;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class CookieConsentTests : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private const string StorageKey = "cookieConsentChoice";

    private readonly TestServerFixture _server;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public CookieConsentTests(TestServerFixture server)
    {
        _server = server;
    }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Fact]
    public async Task FirstVisit_ShowsEqualChoicesAndLoadsNoThirdPartyTags()
    {
        var (context, page, thirdPartyRequests) = await OpenSiteAsync();

        Assert.True(await page.IsVisibleAsync("#consentDiv"));
        Assert.Empty(thirdPartyRequests);
        Assert.Equal(0, await page.EvaluateAsync<int>("() => document.querySelectorAll('iframe[src*=\"googletagmanager\"]').length"));

        var accept = await ButtonAppearanceAsync(page, "#consentAcceptButton");
        var decline = await ButtonAppearanceAsync(page, "#consentDeclineButton");
        Assert.Equal(accept, decline);

        await context.CloseAsync();
    }

    [Fact]
    public async Task Accept_LoadsTagManagerAndIsRemembered()
    {
        var (context, page, thirdPartyRequests) = await OpenSiteAsync();

        await page.ClickAsync("#consentAcceptButton");
        await page.WaitForSelectorAsync("#consentDiv", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden });
        Assert.Contains(thirdPartyRequests, url => url.Contains("googletagmanager.com/gtm.js?id="));
        Assert.Equal("accepted", await ReadStoredChoiceAsync(page));

        thirdPartyRequests.Clear();
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load });
        Assert.False(await page.IsVisibleAsync("#consentDiv"));
        Assert.Contains(thirdPartyRequests, url => url.Contains("googletagmanager.com/gtm.js?id="));

        await context.CloseAsync();
    }

    [Fact]
    public async Task Decline_LoadsNoTagsAndIsRemembered()
    {
        var (context, page, thirdPartyRequests) = await OpenSiteAsync();

        await page.ClickAsync("#consentDeclineButton");
        await page.WaitForSelectorAsync("#consentDiv", new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden });
        Assert.Equal("declined", await ReadStoredChoiceAsync(page));

        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load });
        Assert.False(await page.IsVisibleAsync("#consentDiv"));
        Assert.Empty(thirdPartyRequests);

        await context.CloseAsync();
    }

    [Fact]
    public async Task CookieSettings_WithdrawingConsentStopsTagsAndClearsCookies()
    {
        var (context, page, thirdPartyRequests) = await OpenSiteAsync();

        await page.ClickAsync("#consentAcceptButton");
        await page.EvaluateAsync("() => { document.cookie = '_ga=GA1.1.1; path=/'; document.cookie = '_gcl_au=1.1.1; path=/'; document.cookie = 'unrelated=1; path=/'; }");

        await page.ClickAsync("#cookieSettingsButton");
        Assert.True(await page.IsVisibleAsync("#consentDiv"));
        Assert.Equal("consentAcceptButton", await page.EvaluateAsync<string>("() => document.activeElement.id"));

        thirdPartyRequests.Clear();
        await page.EvaluateAsync("() => { window.beforeReload = true; }");
        await page.ClickAsync("#consentDeclineButton");
        await page.WaitForFunctionAsync("() => !window.beforeReload && document.readyState === 'complete'");

        Assert.Empty(thirdPartyRequests);
        Assert.False(await page.IsVisibleAsync("#consentDiv"));
        Assert.Equal("declined", await ReadStoredChoiceAsync(page));
        Assert.Equal("unrelated=1", await page.EvaluateAsync<string>("() => document.cookie"));

        await page.ClickAsync("#cookieSettingsButton");
        await page.ClickAsync("#consentAcceptButton");
        Assert.Contains(thirdPartyRequests, url => url.Contains("googletagmanager.com/gtm.js?id="));
        Assert.Equal("accepted", await ReadStoredChoiceAsync(page));

        await context.CloseAsync();
    }

    [Fact]
    public async Task FormerImpliedConsent_IsNotHonored()
    {
        Assert.NotNull(_browser);
        var context = await _browser.NewContextAsync();
        await context.AddInitScriptAsync("if (!sessionStorage.getItem('seeded')) { sessionStorage.setItem('seeded', '1'); localStorage.setItem('consentOKClicked', 'true'); }");
        var (page, thirdPartyRequests) = await OpenPageAsync(context);

        Assert.True(await page.IsVisibleAsync("#consentDiv"));
        Assert.Empty(thirdPartyRequests);
        Assert.Null(await page.EvaluateAsync<string?>("() => localStorage.getItem('consentOKClicked')"));

        await context.CloseAsync();
    }

    private async Task<(IBrowserContext Context, IPage Page, List<string> ThirdPartyRequests)> OpenSiteAsync()
    {
        Assert.NotNull(_browser);
        var context = await _browser.NewContextAsync();
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
