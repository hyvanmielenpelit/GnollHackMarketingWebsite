using Microsoft.Playwright;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class ContentSectionTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _server;

    public ContentSectionTests(TestServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task ShareImage_TagsAndFileMatchSpecification()
    {
        const string imagePath = "/img/gnollhack-open-graph-v2.jpg";
        var (context, page) = await _server.OpenPageAsync(1366, 768);
        await using var contextScope = context;

        var ogImage = await page.Locator("meta[property='og:image']").GetAttributeAsync("content");
        var twitterImage = await page.Locator("meta[name='twitter:image']").GetAttributeAsync("content");
        var jsonLdImage = await page.EvaluateAsync<string>(
            "() => JSON.parse(document.querySelector('script[type=\"application/ld+json\"]').textContent).image");

        Assert.EndsWith(imagePath, ogImage);
        Assert.EndsWith(imagePath, twitterImage);
        Assert.EndsWith(imagePath, jsonLdImage);
        Assert.DoesNotContain("gnollhack-open-graph-new.jpg", await page.ContentAsync());

        var response = await page.APIRequest.GetAsync(new Uri(new Uri(_server.ServerAddress), imagePath).ToString());
        Assert.Equal(200, response.Status);
        Assert.True((await response.BodyAsync()).Length <= 150_000, "Share image is larger than 150 KB");

        var size = await page.EvaluateAsync<int[]>(@"src => new Promise((resolve, reject) => {
            const image = new Image();
            image.onload = () => resolve([image.naturalWidth, image.naturalHeight]);
            image.onerror = () => reject(new Error('Share image did not decode'));
            image.src = src;
        })", imagePath);
        Assert.Equal(new[] { 1200, 630 }, size);
    }

    [Fact]
    public async Task ResourceCards_HaveAccessibleSemanticsAndMinimumTargetSize()
    {
        foreach (var (width, height) in new[] { (360, 800), (1920, 1080) })
        {
            var (context, page) = await _server.OpenPageAsync(width, height);
            await using var contextScope = context;

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
        }
    }

    [Fact]
    public async Task DownloadBadges_HaveVisibleCaptionsAndSymmetricRows()
    {
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
            var (context, page) = await _server.OpenPageAsync(width, height);
            await using var contextScope = context;

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
        }
    }

    [Fact]
    public async Task FeatureCards_HaveAlignedListsAndDarkFantasyStickyHeader()
    {
        foreach (var (width, height) in new[] { (360, 800), (1920, 1080) })
        {
            var (context, page) = await _server.OpenPageAsync(width, height);
            await using var contextScope = context;

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
        }
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
}
