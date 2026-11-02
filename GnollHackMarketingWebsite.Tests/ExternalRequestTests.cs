using Microsoft.Playwright;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class ExternalRequestTests : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private readonly TestServerFixture _server;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public ExternalRequestTests(TestServerFixture server)
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
    public async Task FreshPageLoad_RequestsNothingFromOtherHosts()
    {
        Assert.NotNull(_browser);
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var siteOrigin = new Uri(_server.ServerAddress).GetLeftPart(UriPartial.Authority);
        var externalRequests = new List<string>();
        await context.RouteAsync("**/*", async route =>
        {
            var url = route.Request.Url;
            if (url.StartsWith(siteOrigin, StringComparison.OrdinalIgnoreCase) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                await route.ContinueAsync();
                return;
            }

            externalRequests.Add(url);
            await route.AbortAsync();
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        // Scrolling triggers lazy-loaded images and every declared web font
        await page.EvaluateAsync(@"async () => {
            for (let y = 0; y < document.documentElement.scrollHeight; y += window.innerHeight) {
                window.scrollTo(0, y);
                await new Promise(resolve => setTimeout(resolve, 50));
            }
            await document.fonts.ready;
        }");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Empty(externalRequests);
        await context.CloseAsync();
    }

    [Fact]
    public async Task SelfHostedFonts_LoadFromSite()
    {
        Assert.NotNull(_browser);
        var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.Load });

        var loadedFonts = await page.EvaluateAsync<string[]>(@"async () => {
            await document.fonts.ready;
            return [...document.fonts].filter(font => font.status === 'loaded').map(font => font.family.replaceAll('""', ''));
        }");
        Assert.Contains("Cinzel", loadedFonts);
        Assert.Contains("Lato", loadedFonts);

        foreach (var fontFile in new[] { "cinzel-latin-wght-normal.woff2", "lato-latin-400-normal.woff2", "lato-latin-400-italic.woff2", "lato-latin-700-normal.woff2" })
        {
            var response = await page.APIRequest.GetAsync($"{_server.ServerAddress.TrimEnd('/')}/fonts/{fontFile}");
            Assert.True(response.Ok, $"{fontFile} returned {response.Status}");
        }

        await context.CloseAsync();
    }
}
