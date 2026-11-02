using Microsoft.Playwright;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class CarouselTests : IClassFixture<TestServerFixture>
{
    private const string CarouselGeometryScript = @"() => {
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
        }";

    private readonly TestServerFixture _server;

    public CarouselTests(TestServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task Carousel_ModalAndVideoPosters_HaveHDFacadesAndAriaAttributes()
    {
        var (context, page) = await _server.OpenPageAsync(1920, 1080);
        await using var contextScope = context;

        // 1. Check modal dialog ARIA accessibility attributes
        var modal = page.Locator("#fullscreenModal");
        await Assertions.Expect(modal).ToHaveAttributeAsync("role", "dialog");
        await Assertions.Expect(modal).ToHaveAttributeAsync("aria-modal", "true");
        await Assertions.Expect(modal).ToHaveAttributeAsync("aria-label", "Fullscreen Media Gallery");

        // 2. Check locally served HD poster images on video items
        var videoPosters = page.Locator(".video-poster-img");
        var posterCount = await videoPosters.CountAsync();
        Assert.True(posterCount > 0, "No video poster images found on page.");

        for (int i = 0; i < posterCount; i++)
        {
            var src = await videoPosters.Nth(i).GetAttributeAsync("src");
            Assert.NotNull(src);
            Assert.StartsWith("/img/video-poster-", src);

            var posterResponse = await page.APIRequest.GetAsync(new Uri(new Uri(_server.ServerAddress), src).ToString());
            Assert.True(posterResponse.Ok, $"Poster {src} returned {posterResponse.Status}");
        }

        // 3. Check play button presence and accessibility label
        var playBtns = page.Locator(".video-play-btn");
        var btnCount = await playBtns.CountAsync();
        Assert.True(btnCount > 0, "No video play buttons found.");
        await Assertions.Expect(playBtns.First).ToHaveAttributeAsync("aria-label", "Play Video");
    }

    [Fact]
    public async Task FullscreenModal_OnMobileViewport_OpensAtRootAndCoversFullViewport()
    {
        var (context, page) = await _server.OpenPageAsync(360, 800);
        await using var contextScope = context;

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
    }

    [Fact]
    public async Task MainCarousel_MaintainsUniform16x9GeometryAndCenteredControls()
    {
        // Test desktop (1920x1080)
        var (desktopContext, desktopPage) = await _server.OpenPageAsync(1920, 1080);
        await using (desktopContext)
        {
            var desktopGeometry = await desktopPage.EvaluateAsync<CarouselGeometry>(CarouselGeometryScript);

            Assert.Equal(35, desktopGeometry.PaddingLeft);
            Assert.Equal(35, desktopGeometry.PaddingRight);
            Assert.Equal(35, desktopGeometry.PaddingTop);
            Assert.Equal(35, desktopGeometry.PaddingBottom);
            Assert.Equal(35, desktopGeometry.PrevWidth);
            Assert.Equal(35, desktopGeometry.NextWidth);

            // Inner aspect ratio should be 16 / 9 (~1.7777) within 0.05 tolerance
            var desktopAspectRatio = desktopGeometry.InnerWidth / desktopGeometry.InnerHeight;
            Assert.InRange(desktopAspectRatio, 1.70, 1.82);
        }

        // Test mobile (360x800)
        var (mobileContext, mobilePage) = await _server.OpenPageAsync(360, 800);
        await using (mobileContext)
        {
            var mobileGeometry = await mobilePage.EvaluateAsync<CarouselGeometry>(CarouselGeometryScript);

            Assert.Equal(25, mobileGeometry.PaddingLeft);
            Assert.Equal(25, mobileGeometry.PaddingRight);
            Assert.Equal(25, mobileGeometry.PaddingTop);
            Assert.Equal(25, mobileGeometry.PaddingBottom);
            Assert.Equal(25, mobileGeometry.PrevWidth);
            Assert.Equal(25, mobileGeometry.NextWidth);

            var mobileAspectRatio = mobileGeometry.InnerWidth / mobileGeometry.InnerHeight;
            Assert.InRange(mobileAspectRatio, 1.70, 1.82);
        }
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
