using Microsoft.Playwright;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class ExternalRequestTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _server;

    public ExternalRequestTests(TestServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task FreshPageLoad_RequestsNothingFromOtherHosts()
    {
        await using var context = await _server.Browser.NewContextAsync(new BrowserNewContextOptions
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
    }

    [Fact]
    public async Task SelfHostedFonts_LoadFromSite()
    {
        await using var context = await _server.Browser.NewContextAsync();
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
    }
}
