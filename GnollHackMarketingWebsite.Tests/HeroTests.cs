using Microsoft.Playwright;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class HeroTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _server;

    public HeroTests(TestServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task Hero_HasOneVisibleH1WithWordmarkAndSlogan()
    {
        var (context, page) = await _server.OpenPageAsync(1366, 768);
        await using var contextScope = context;

        var headings = page.Locator("h1");
        Assert.Equal(1, await headings.CountAsync());
        await Assertions.Expect(headings).ToBeVisibleAsync();

        var text = await headings.InnerTextAsync();
        Assert.Contains("GnollHack", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("The Modern Way to Play NetHack", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(320, 568, "portrait")]
    [InlineData(390, 844, "portrait")]
    [InlineData(767, 1024, "portrait")]
    [InlineData(768, 1024, "landscape")]
    [InlineData(1366, 768, "landscape")]
    public async Task Hero_LoadsArtForViewportWithHighPriority(int width, int height, string expectedArt)
    {
        var (context, page) = await _server.OpenPageAsync(width, height);
        await using var contextScope = context;

        var image = page.Locator(".hero-art img");
        await Assertions.Expect(image).ToHaveAttributeAsync("fetchpriority", "high");
        Assert.Null(await image.GetAttributeAsync("loading"));

        await page.WaitForFunctionAsync("() => document.querySelector('.hero-art img').currentSrc !== ''");
        var currentSrc = await image.EvaluateAsync<string>("img => img.currentSrc");
        Assert.Contains($"/img/hero-ranger-{expectedArt}-", currentSrc);
    }

    [Theory]
    [InlineData(320, 568)]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    [InlineData(1366, 768)]
    [InlineData(1920, 1080)]
    public async Task Hero_WordmarkFitsWithinViewport(int width, int height)
    {
        var (context, page) = await _server.OpenPageAsync(width, height);
        await using var contextScope = context;
        await page.EvaluateAsync("() => document.fonts.ready.then(() => true)");

        var bounds = await page.EvaluateAsync<double[]>(@"() => {
            const range = document.createRange();
            range.selectNodeContents(document.querySelector('.hero-wordmark'));
            const rect = range.getBoundingClientRect();
            return [rect.left, rect.right];
        }");

        Assert.True(bounds[0] >= 0 && bounds[1] <= width,
            $"Wordmark spans {bounds[0]:F1}px to {bounds[1]:F1}px at {width}x{height}");
    }

    [Theory]
    [InlineData(320, 568)]
    [InlineData(390, 844)]
    [InlineData(844, 390)]
    [InlineData(1366, 768)]
    public async Task Hero_PrimaryButtonIsOnFirstScreen(int width, int height)
    {
        var (context, page) = await _server.OpenPageAsync(width, height);
        await using var contextScope = context;
        await page.EvaluateAsync("() => document.fonts.ready.then(() => true)");

        var box = await page.Locator(".hero-cta-primary").BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box.X >= 0 && box.Y >= 0 && box.X + box.Width <= width && box.Y + box.Height <= height,
            $"Primary button is at ({box.X:F1}, {box.Y:F1}) sized {box.Width:F1}x{box.Height:F1} at {width}x{height}");
    }
}
