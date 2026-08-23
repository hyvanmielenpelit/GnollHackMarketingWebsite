using Microsoft.Playwright;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class ResponsiveLayoutTests : IClassFixture<TestServerFixture>, IAsyncLifetime
{
    private readonly TestServerFixture _server;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public ResponsiveLayoutTests(TestServerFixture server)
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

    [Theory]
    [InlineData(320, 568)]   // iPhone SE 1st gen
    [InlineData(360, 800)]   // Standard Android narrow
    [InlineData(390, 844)]   // iPhone 12/13/14
    [InlineData(430, 932)]   // iPhone 14/15/16 Pro Max
    [InlineData(768, 1024)]  // iPad / Tablet portrait
    [InlineData(1080, 1920)] // Full HD Portrait
    [InlineData(1920, 1080)] // Desktop 1080p
    public async Task Viewport_HasZeroHorizontalOverflow(int width, int height)
    {
        Assert.NotNull(_browser);
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });

        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(_server.ServerAddress, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        Assert.NotNull(response);
        Assert.True(response.Ok, $"Failed to load {_server.ServerAddress} - Status: {response.Status}");

        // Check horizontal overflow: scrollWidth must equal clientWidth / innerWidth
        var isOverflowing = await page.EvaluateAsync<bool>(@"() => {
            const scrollWidth = document.documentElement.scrollWidth;
            const innerWidth = window.innerWidth;
            return scrollWidth > innerWidth;
        }");

        Assert.False(isOverflowing, $"Page has horizontal overflow at {width}x{height} viewport (scrollWidth > innerWidth)");
        await context.CloseAsync();
    }

    [Fact]
    public async Task Typography_ComputedStyles_ScaleProportionallyOnMobileAndDesktop()
    {
        Assert.NotNull(_browser);

        // Test Narrow Mobile (360px)
        var mobileContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 360, Height = 800 }
        });
        var mobilePage = await mobileContext.NewPageAsync();
        await mobilePage.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var mobileH2FontSize = await mobilePage.EvaluateAsync<string>(@"() => {
            const h2 = document.querySelector('h2');
            return h2 ? window.getComputedStyle(h2).fontSize : '';
        }");

        // At 360px, h2 (clamp 38px, 9.5vw, 48px) resolves to 38px
        Assert.Equal("38px", mobileH2FontSize);
        await mobileContext.CloseAsync();

        // Test Standard Desktop (1920px)
        var desktopContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });
        var desktopPage = await desktopContext.NewPageAsync();
        await desktopPage.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var desktopH2FontSize = await desktopPage.EvaluateAsync<string>(@"() => {
            const h2 = document.querySelector('h2');
            return h2 ? window.getComputedStyle(h2).fontSize : '';
        }");

        // At 1920px, h2 in the 1500px-2599px media query resolves to 53px
        Assert.Equal("53px", desktopH2FontSize);
        await desktopContext.CloseAsync();
    }
}
