using Microsoft.Playwright;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class ResponsiveLayoutTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _server;

    public ResponsiveLayoutTests(TestServerFixture server)
    {
        _server = server;
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
        await using var context = await _server.Browser.NewContextAsync(new BrowserNewContextOptions
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
    }

    [Theory]
    [InlineData(1280, 800)]
    [InlineData(320, 568)]
    public async Task Viewport_HasZeroHorizontalOverflowAtDoubleFontSize(int width, int height)
    {
        var (context, page) = await _server.OpenPageAsync(width, height);
        await using var contextScope = context;
        await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "html { font-size: 200%; }" });

        var scrollWidth = await page.EvaluateAsync<double>("() => document.documentElement.scrollWidth");

        Assert.True(scrollWidth <= width, $"Page is {scrollWidth}px wide at 200% font size in a {width}px viewport");
    }

    [Fact]
    public async Task Typography_ComputedStyles_ScaleProportionallyOnMobileAndDesktop()
    {
        // Test Narrowest Mobile (320px)
        var (mobileContext, mobilePage) = await _server.OpenPageAsync(320, 568);
        await using (mobileContext)
        {
            var mobileH2FontSize = await mobilePage.EvaluateAsync<double>(@"() => {
            const h2 = document.querySelector('h2');
            return h2 ? parseFloat(window.getComputedStyle(h2).fontSize) : 0;
        }");

            // At 320px, the h2 clamp sits at its 38px minimum
            Assert.InRange(mobileH2FontSize, 38, 38.1);
        }

        // Test Standard Desktop (1920px)
        var (desktopContext, desktopPage) = await _server.OpenPageAsync(1920, 1080);
        await using (desktopContext)
        {
            var desktopH2FontSize = await desktopPage.EvaluateAsync<double>(@"() => {
            const h2 = document.querySelector('h2');
            return h2 ? parseFloat(window.getComputedStyle(h2).fontSize) : 0;
        }");

            // At 1920px, the wide-screen h2 line resolves to about 53px
            Assert.InRange(desktopH2FontSize, 52.5, 53.5);
        }
    }

    [Theory]
    [InlineData(320, 568)]
    [InlineData(1366, 768)]
    public async Task Typography_HasNoTextBelowFourteenPixels(int width, int height)
    {
        var (context, page) = await _server.OpenPageAsync(width, height);
        await using var contextScope = context;

        var tooSmall = await page.EvaluateAsync<string[]>(@"() =>
            Array.from(document.body.querySelectorAll('*'))
                .filter(el => !el.closest('.visually-hidden, .skip-link, script, style, noscript'))
                .filter(el => Array.from(el.childNodes).some(n => n.nodeType === Node.TEXT_NODE && n.textContent.trim().length > 0))
                .filter(el => parseFloat(getComputedStyle(el).fontSize) < 14)
                .map(el => `${el.tagName}.${el.className}: ${getComputedStyle(el).fontSize}`)");

        Assert.Empty(tooSmall);
    }

    [Fact]
    public async Task DwarfMonk_ContentStaysClearOfArtworkOnWideScreens()
    {
        foreach (var (width, height) in new[] { (1200, 800), (1440, 900), (1920, 1080), (2560, 1440), (3440, 1440) })
        {
            var (context, page) = await _server.OpenPageAsync(width, height);
            await using var contextScope = context;

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
        }
    }

    private class SectionClearanceMetric
    {
        public string Selector { get; set; } = "";
        public double Clearance { get; set; }
    }
}
