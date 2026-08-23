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

    [Fact]
    public async Task Carousel_ModalAndVideoPosters_HaveHDFacadesAndAriaAttributes()
    {
        Assert.NotNull(_browser);
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // 1. Check modal dialog ARIA accessibility attributes
        var modal = page.Locator("#fullscreenModal");
        await Assertions.Expect(modal).ToHaveAttributeAsync("role", "dialog");
        await Assertions.Expect(modal).ToHaveAttributeAsync("aria-modal", "true");
        await Assertions.Expect(modal).ToHaveAttributeAsync("aria-label", "Fullscreen Media Gallery");

        // 2. Check HD poster image URLs (maxresdefault.jpg) on video items
        var videoPosters = page.Locator(".video-poster-img");
        var posterCount = await videoPosters.CountAsync();
        Assert.True(posterCount > 0, "No video poster images found on page.");

        for (int i = 0; i < posterCount; i++)
        {
            var src = await videoPosters.Nth(i).GetAttributeAsync("src");
            Assert.NotNull(src);
            Assert.Contains("maxresdefault.jpg", src);
        }

        // 3. Check play button presence and accessibility label
        var playBtns = page.Locator(".video-play-btn");
        var btnCount = await playBtns.CountAsync();
        Assert.True(btnCount > 0, "No video play buttons found.");
        await Assertions.Expect(playBtns.First).ToHaveAttributeAsync("aria-label", "Play Video");

        await context.CloseAsync();
    }

    [Fact]
    public async Task FullscreenModal_OnMobileViewport_OpensAtRootAndCoversFullViewport()
    {
        Assert.NotNull(_browser);
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 360, Height = 800 }
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Click first carousel item to open modal
        var firstItem = page.Locator("#carouselComponent .carousel-item.active img, #carouselComponent .carousel-item.active .video-poster-container").First;
        await firstItem.ClickAsync();

        // Wait for modal to become visible
        var modal = page.Locator("#fullscreenModal");
        await Assertions.Expect(modal).ToBeVisibleAsync();

        // Verify modal bounding box covers full 360x800 viewport
        var modalBox = await modal.BoundingBoxAsync();
        Assert.NotNull(modalBox);
        Assert.Equal(0, modalBox.X);
        Assert.Equal(0, modalBox.Y);
        Assert.Equal(360, modalBox.Width);
        Assert.Equal(800, modalBox.Height);

        // Verify modal is direct child of body (not trapped inside a section with backdrop-filter)
        var isDirectBodyChild = await page.EvaluateAsync<bool>(@"() => {
            const modal = document.getElementById('fullscreenModal');
            return modal && modal.parentNode === document.body;
        }");
        Assert.True(isDirectBodyChild, "fullscreenModal must be a direct child of document.body");

        // Verify close button closes modal
        var closeBtn = page.Locator("#fullscreenModal .btn-close");
        await closeBtn.ClickAsync();
        await Assertions.Expect(modal).ToBeHiddenAsync();

        await context.CloseAsync();
    }

    [Fact]
    public async Task MainCarousel_MaintainsUniform16x9GeometryAndCenteredControls()
    {
        Assert.NotNull(_browser);

        // Test desktop (1920x1080)
        var desktopContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });
        var desktopPage = await desktopContext.NewPageAsync();
        await desktopPage.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var desktopGeometry = await desktopPage.EvaluateAsync<CarouselGeometry>(@"() => {
            const container = document.getElementById('carouselComponent');
            const inner = container ? container.querySelector('.carousel-inner') : null;
            const prev = container ? container.querySelector('.main-carousel-control-prev') : null;
            const next = container ? container.querySelector('.main-carousel-control-next') : null;
            const style = container ? window.getComputedStyle(container) : null;
            const innerRect = inner ? inner.getBoundingClientRect() : null;
            const prevRect = prev ? prev.getBoundingClientRect() : null;
            const nextRect = next ? next.getBoundingClientRect() : null;

            return {
                paddingLeft: style ? parseFloat(style.paddingLeft) : 0,
                paddingRight: style ? parseFloat(style.paddingRight) : 0,
                paddingTop: style ? parseFloat(style.paddingTop) : 0,
                paddingBottom: style ? parseFloat(style.paddingBottom) : 0,
                innerWidth: innerRect ? innerRect.width : 0,
                innerHeight: innerRect ? innerRect.height : 0,
                prevWidth: prevRect ? prevRect.width : 0,
                nextWidth: nextRect ? nextRect.width : 0
            };
        }");

        Assert.Equal(35, desktopGeometry.PaddingLeft);
        Assert.Equal(35, desktopGeometry.PaddingRight);
        Assert.Equal(35, desktopGeometry.PaddingTop);
        Assert.Equal(35, desktopGeometry.PaddingBottom);
        Assert.Equal(35, desktopGeometry.PrevWidth);
        Assert.Equal(35, desktopGeometry.NextWidth);

        // Inner aspect ratio should be 16 / 9 (~1.7777) within 0.05 tolerance
        var desktopAspectRatio = desktopGeometry.InnerWidth / desktopGeometry.InnerHeight;
        Assert.InRange(desktopAspectRatio, 1.70, 1.82);

        await desktopContext.CloseAsync();

        // Test mobile (360x800)
        var mobileContext = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 360, Height = 800 }
        });
        var mobilePage = await mobileContext.NewPageAsync();
        await mobilePage.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var mobileGeometry = await mobilePage.EvaluateAsync<CarouselGeometry>(@"() => {
            const container = document.getElementById('carouselComponent');
            const inner = container ? container.querySelector('.carousel-inner') : null;
            const prev = container ? container.querySelector('.main-carousel-control-prev') : null;
            const next = container ? container.querySelector('.main-carousel-control-next') : null;
            const style = container ? window.getComputedStyle(container) : null;
            const innerRect = inner ? inner.getBoundingClientRect() : null;
            const prevRect = prev ? prev.getBoundingClientRect() : null;
            const nextRect = next ? next.getBoundingClientRect() : null;

            return {
                paddingLeft: style ? parseFloat(style.paddingLeft) : 0,
                paddingRight: style ? parseFloat(style.paddingRight) : 0,
                paddingTop: style ? parseFloat(style.paddingTop) : 0,
                paddingBottom: style ? parseFloat(style.paddingBottom) : 0,
                innerWidth: innerRect ? innerRect.width : 0,
                innerHeight: innerRect ? innerRect.height : 0,
                prevWidth: prevRect ? prevRect.width : 0,
                nextWidth: nextRect ? nextRect.width : 0
            };
        }");

        Assert.Equal(25, mobileGeometry.PaddingLeft);
        Assert.Equal(25, mobileGeometry.PaddingRight);
        Assert.Equal(25, mobileGeometry.PaddingTop);
        Assert.Equal(25, mobileGeometry.PaddingBottom);
        Assert.Equal(25, mobileGeometry.PrevWidth);
        Assert.Equal(25, mobileGeometry.NextWidth);

        var mobileAspectRatio = mobileGeometry.InnerWidth / mobileGeometry.InnerHeight;
        Assert.InRange(mobileAspectRatio, 1.70, 1.82);

        await mobileContext.CloseAsync();
    }

    private class CarouselGeometry
    {
        public double PaddingLeft { get; set; }
        public double PaddingRight { get; set; }
        public double PaddingTop { get; set; }
        public double PaddingBottom { get; set; }
        public double InnerWidth { get; set; }
        public double InnerHeight { get; set; }
        public double PrevWidth { get; set; }
        public double NextWidth { get; set; }
    }
}

