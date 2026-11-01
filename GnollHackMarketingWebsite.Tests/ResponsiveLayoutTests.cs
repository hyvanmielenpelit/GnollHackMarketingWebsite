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

    [Fact]
    public async Task ResourceCards_HaveAccessibleSemanticsAndMinimumTargetSize()
    {
        Assert.NotNull(_browser);

        foreach (var (width, height) in new[] { (360, 800), (1920, 1080) })
        {
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            // Two lists that keep list semantics under list-style: none
            var grids = page.Locator("ul.resource-grid");
            Assert.Equal(2, await grids.CountAsync());
            for (int i = 0; i < 2; i++)
            {
                await Assertions.Expect(grids.Nth(i)).ToHaveAttributeAsync("role", "list");
            }

            // Six cards, one of them Gnoll Overseer
            var cards = page.Locator(".resource-card");
            Assert.Equal(6, await cards.CountAsync());
            Assert.Equal(1, await page.Locator(".resource-link[href='https://overseer.gnollhack.com']").CountAsync());

            // Every link is described by an existing .resource-desc element
            var allDescribed = await page.EvaluateAsync<bool>(@"() => {
                const links = Array.from(document.querySelectorAll('.resource-link'));
                return links.length === 6 && links.every(link => {
                    const id = link.getAttribute('aria-describedby');
                    const desc = id ? document.getElementById(id) : null;
                    return desc !== null && desc.classList.contains('resource-desc') && desc.textContent.trim().length > 0;
                });
            }");
            Assert.True(allDescribed, $"Every .resource-link must have an aria-describedby resolving to a .resource-desc at {width}x{height}");

            // WCAG 2.5.5 target size
            for (int i = 0; i < 6; i++)
            {
                var box = await cards.Nth(i).BoundingBoxAsync();
                Assert.NotNull(box);
                Assert.True(box.Height >= 44, $"Resource card {i} is {box.Height}px tall at {width}x{height}; expected >= 44px");
            }

            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task DwarfMonk_ContentStaysClearOfArtworkOnWideScreens()
    {
        Assert.NotNull(_browser);

        foreach (var (width, height) in new[] { (1200, 800), (1440, 900), (1920, 1080), (2560, 1440), (3440, 1440) })
        {
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            var clearances = await page.EvaluateAsync<SectionClearanceMetric[]>(@"() => {
                const specs = [
                    { selector: '.orc-assassin', side: 'left', extent: 0.455 },
                    { selector: '.orc-hunter', side: 'right', extent: 0.445 },
                    { selector: '.female-mage', side: 'left', extent: 0.445 },
                    { selector: '.barbarian', side: 'right', extent: 0.385 },
                    { selector: '.dwarf-monk', side: 'left', extent: 0.552 }
                ];

                return specs.map(spec => {
                    const section = document.querySelector(spec.selector);
                    const s = section.getBoundingClientRect();
                    const style = getComputedStyle(section);
                    const bgSize = style.backgroundSize.trim();
                    const artworkWidth = bgSize === 'cover'
                        ? Math.max(s.width, s.height * 16 / 9)
                        : (bgSize.startsWith('100%') ? s.width : Math.max(s.width, s.height * 16 / 9));

                    const items = Array.from(section.querySelectorAll(
                        'h2, .section-lead, .download-panel, .intro-card, .main-carousel-container, .feature-card, .resource-card'
                    ));

                    let clearance;
                    if (spec.side === 'left') {
                        const posX = style.backgroundPositionX.endsWith('px') ? (parseFloat(style.backgroundPositionX) || 0) : 0;
                        const characterRight = s.left + posX + artworkWidth * spec.extent;
                        const contentLeft = Math.min(...items.map(e => e.getBoundingClientRect().left));
                        clearance = contentLeft - characterRight;
                    } else {
                        const characterLeft = s.right - artworkWidth * spec.extent;
                        const contentRight = Math.max(...items.map(e => e.getBoundingClientRect().right));
                        clearance = characterLeft - contentRight;
                    }

                    return {
                        selector: spec.selector,
                        clearance
                    };
                });
            }");

            Assert.Equal(5, clearances.Length);
            foreach (var metric in clearances)
            {
                Assert.True(metric.Clearance >= 24,
                    $"Section {metric.Selector} has {metric.Clearance:F1}px artwork clearance at {width}x{height}; expected >= 24px");
            }

            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task DownloadBadges_HaveVisibleCaptionsAndSymmetricRows()
    {
        Assert.NotNull(_browser);

        var cases = new (int Width, int Height, int[] ExpectedRowCounts)[]
        {
            (360, 800, new[] { 1, 1, 1, 1 }),
            (768, 1024, new[] { 2, 2 }),
            (1024, 768, new[] { 2, 2 }),
            (1440, 900, new[] { 2, 2 }),
            (1920, 1080, new[] { 2, 2 }),
            (2560, 1440, new[] { 2, 2 })
        };

        foreach (var (width, height, expectedRows) in cases)
        {
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            // Visible platform labels on every viewport
            var captions = page.Locator(".download-platform");
            Assert.Equal(4, await captions.CountAsync());
            for (int i = 0; i < 4; i++)
            {
                await Assertions.Expect(captions.Nth(i)).ToBeVisibleAsync();
            }

            var layout = await page.EvaluateAsync<BadgeLayoutResult>(@"() => {
                const items = Array.from(document.querySelectorAll('.download-grid .download-item'));
                const rows = [];
                for (const item of items) {
                    const top = item.getBoundingClientRect().top;
                    const existing = rows.find(r => Math.abs(r.top - top) < 8);
                    if (existing) {
                        existing.count += 1;
                    } else {
                        rows.push({ top, count: 1 });
                    }
                }
                rows.sort((a, b) => a.top - b.top);

                const badges = Array.from(document.querySelectorAll('.download-grid img.shiny-borders')).map(img => {
                    const rect = img.getBoundingClientRect();
                    const style = getComputedStyle(img);
                    const borderX = (parseFloat(style.borderLeftWidth) || 0) + (parseFloat(style.borderRightWidth) || 0);
                    const borderY = (parseFloat(style.borderTopWidth) || 0) + (parseFloat(style.borderBottomWidth) || 0);
                    const contentWidth = rect.width - borderX;
                    const contentHeight = rect.height - borderY;
                    const attrWidth = parseFloat(img.getAttribute('width') || '0');
                    const attrHeight = parseFloat(img.getAttribute('height') || '1');
                    return {
                        outerWidth: rect.width,
                        outerHeight: rect.height,
                        renderedRatio: contentHeight > 0 ? contentWidth / contentHeight : 0,
                        intrinsicRatio: attrWidth / attrHeight
                    };
                });

                return {
                    rowCounts: rows.map(r => r.count),
                    badges
                };
            }");

            Assert.Equal(expectedRows, layout.RowCounts);
            Assert.Equal(4, layout.Badges.Length);

            foreach (var badge in layout.Badges)
            {
                Assert.True(Math.Abs(badge.RenderedRatio - badge.IntrinsicRatio) < 0.08,
                    $"Badge aspect ratio {badge.RenderedRatio:F3} drifted from {badge.IntrinsicRatio:F3} at {width}x{height}");
            }

            if (width >= 768)
            {
                var heights = layout.Badges.Select(b => b.OuterHeight).ToArray();
                Assert.True(heights.Max() - heights.Min() <= 1.5, $"Desktop badge heights should match at {width}x{height}");
            }
            else
            {
                var widths = layout.Badges.Select(b => b.OuterWidth).ToArray();
                Assert.True(widths.Max() - widths.Min() <= 1.5, $"Mobile badge widths should match at {width}x{height}");
            }

            await context.CloseAsync();
        }
    }

    [Fact]
    public async Task FeatureCards_HaveAlignedListsAndDarkFantasyStickyHeader()
    {
        Assert.NotNull(_browser);

        foreach (var (width, height) in new[] { (360, 800), (1920, 1080) })
        {
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(_server.ServerAddress, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            // Skip link and sticky dark-fantasy header
            var skipLink = page.Locator("a.skip-link[href='#main-content']");
            Assert.Equal(1, await skipLink.CountAsync());

            var header = page.Locator("header.site-header");
            await Assertions.Expect(header).ToBeVisibleAsync();

            var headerCheck = await page.EvaluateAsync<HeaderAndAlignmentResult>(@"() => {
                const header = document.querySelector('header.site-header');
                const brand = document.querySelector('.brand-title');
                const headerStyle = getComputedStyle(header);
                const brandStyle = getComputedStyle(brand);

                const parseRgb = (str) => {
                    const m = str.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
                    return m ? [parseInt(m[1], 10), parseInt(m[2], 10), parseInt(m[3], 10)] : [0, 0, 0];
                };

                const bgRgb = parseRgb(headerStyle.backgroundColor);
                const inkRgb = parseRgb(brandStyle.color);

                const listLeftSpread = (selector) => {
                    const lists = Array.from(document.querySelectorAll(selector));
                    const lefts = lists.map(el => el.getBoundingClientRect().left);
                    return lefts.length >= 2 ? Math.max(...lefts) - Math.min(...lefts) : -1;
                };

                const screenshotSlides = Array.from(document.querySelectorAll('.main-carousel-img'));
                const allScreenshotsOperable = screenshotSlides.length > 0 && screenshotSlides.every(img =>
                    img.getAttribute('role') === 'button' && img.getAttribute('tabindex') === '0');

                return {
                    position: headerStyle.position,
                    headerR: bgRgb[0],
                    headerG: bgRgb[1],
                    headerB: bgRgb[2],
                    inkR: inkRgb[0],
                    inkG: inkRgb[1],
                    inkB: inkRgb[2],
                    mageSpread: listLeftSpread('.female-mage .feature-list'),
                    barbarianSpread: listLeftSpread('.barbarian .feature-list'),
                    allScreenshotsOperable
                };
            }");

            Assert.Equal("sticky", headerCheck.Position);
            Assert.True(headerCheck.HeaderR < 40 && headerCheck.HeaderG < 35 && headerCheck.HeaderB < 35,
                $"Expected smoked-obsidian header background at {width}x{height}, got rgb({headerCheck.HeaderR}, {headerCheck.HeaderG}, {headerCheck.HeaderB})");
            Assert.True(headerCheck.InkR > 220 && headerCheck.InkG > 210 && headerCheck.InkB > 190,
                $"Expected high-contrast warm ivory brand ink at {width}x{height}, got rgb({headerCheck.InkR}, {headerCheck.InkG}, {headerCheck.InkB})");
            Assert.InRange(headerCheck.MageSpread, 0, 1.0);
            Assert.InRange(headerCheck.BarbarianSpread, 0, 1.0);
            Assert.True(headerCheck.AllScreenshotsOperable, "All screenshot carousel items must have role='button' and tabindex='0'");

            await context.CloseAsync();
        }
    }

    private class SectionClearanceMetric
    {
        public string Selector { get; set; } = "";
        public double Clearance { get; set; }
    }

    private class BadgeLayoutResult
    {
        public int[] RowCounts { get; set; } = [];
        public BadgeMetric[] Badges { get; set; } = [];
    }

    private class BadgeMetric
    {
        public double OuterWidth { get; set; }
        public double OuterHeight { get; set; }
        public double RenderedRatio { get; set; }
        public double IntrinsicRatio { get; set; }
    }

    private class HeaderAndAlignmentResult
    {
        public string Position { get; set; } = "";
        public int HeaderR { get; set; }
        public int HeaderG { get; set; }
        public int HeaderB { get; set; }
        public int InkR { get; set; }
        public int InkG { get; set; }
        public int InkB { get; set; }
        public double MageSpread { get; set; }
        public double BarbarianSpread { get; set; }
        public bool AllScreenshotsOperable { get; set; }
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

