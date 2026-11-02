using System.Text.RegularExpressions;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class CssSpecificationTests
{
    private static string GetWwwRootPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        // Traverse up to find the solution or website folder
        var dir = new DirectoryInfo(currentDir);
        while (dir != null)
        {
            var target = Path.Combine(dir.FullName, "GnollHackMarketingWebsite", "wwwroot", "css");
            if (Directory.Exists(target))
            {
                return target;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate wwwroot/css directory.");
    }

    private static string ReadCss(string cssDir, string fileName) =>
        File.ReadAllText(Path.Combine(cssDir, fileName)).Replace("\r\n", "\n");

    [Fact]
    public void Typography_MatchesSpecificationAcrossBreakpoints()
    {
        var cssDir = GetWwwRootPath();
        var site2Css = ReadCss(cssDir, "site2.css");

        // Text tiers
        Assert.Contains("--gh-font-caption: min(1rem, max(0.875rem, calc(0.698rem + 0.189vw)));", site2Css);
        Assert.Contains("--gh-font-small: min(1.125rem, max(1rem, calc(0.823rem + 0.189vw)));", site2Css);
        Assert.Contains("--gh-font-body: min(1.5rem, max(1.1875rem, calc(0.745rem + 0.472vw)));", site2Css);

        // Headings
        Assert.Contains("font-size: min(4.5625rem, max(clamp(2.75rem, 2.125rem + 3.125vw, 3.625rem), calc(2.3rem + 1.415vw)));", site2Css);
        Assert.Contains("font-size: min(3.8125rem, max(clamp(2.375rem, 1.929rem + 2.232vw, 3rem), calc(1.85rem + 1.226vw)));", site2Css);
        Assert.Contains("font-size: min(2.6875rem, max(clamp(1.6875rem, 1.375rem + 1.5625vw, 2.125rem), calc(1.329rem + 0.849vw)));", site2Css);
    }

    [Fact]
    public void Typography_HasNoPixelFontSizes()
    {
        var cssDir = GetWwwRootPath();
        var site2Css = ReadCss(cssDir, "site2.css");

        var pixelSizes = Regex.Matches(site2Css, @"font-size:[^;}]*\dpx[^;}]*").Select(m => m.Value).ToArray();

        Assert.Empty(pixelSizes);
    }

    [Fact]
    public void ResourceLink_VisitedColorMatchesNormalColor()
    {
        var cssDir = GetWwwRootPath();
        var site2Css = ReadCss(cssDir, "site2.css");

        Assert.Matches(@"\.resource-link \{[^}]*\bcolor: var\(--gh-gold\);", site2Css);
        Assert.Matches(@"\.resource-link:visited \{\s*color: var\(--gh-gold\);", site2Css);
    }

    [Fact]
    public void Links_HaveNoUnderlineAndLightenedGoldHover()
    {
        var cssDir = GetWwwRootPath();
        var site2Css = ReadCss(cssDir, "site2.css");

        Assert.DoesNotContain("underline", site2Css);
        Assert.Matches(@"--gh-gold-hover: #fae8bb;", site2Css);
        Assert.Matches(@"\.inline-accent:hover \{\s*color: var\(--gh-gold-hover\);", site2Css);
        Assert.Matches(@"\.resource-link:hover \{\s*color: var\(--gh-gold-hover\);", site2Css);
        Assert.Matches(@"\.text-container a:hover \{\s*color: var\(--gh-gold-hover\);", site2Css);
        Assert.Matches(@"\.site-footer a:hover \{\s*color: var\(--gh-gold-hover\);", site2Css);
        Assert.Matches(@"\.footer-link-button:hover \{\s*color: var\(--gh-gold-hover\);", site2Css);
    }

    [Fact]
    public void Hero_StylesExistInCompiledCss()
    {
        var cssDir = GetWwwRootPath();
        var site2Css = ReadCss(cssDir, "site2.css");

        var heroClasses = new[]
        {
            ".hero", ".hero-art", ".hero-content", ".hero-crest", ".hero-title", ".hero-wordmark",
            ".hero-slogan", ".hero-actions", ".hero-cta", ".hero-cta-primary", ".hero-cta-secondary", ".hero-platforms"
        };
        foreach (var cls in heroClasses)
        {
            Assert.Contains(cls + " {", site2Css);
        }
    }

    [Fact]
    public void MobileSections_DoNotHaveHardcodedHeights()
    {
        var cssDir = GetWwwRootPath();
        var site2Css = File.ReadAllText(Path.Combine(cssDir, "site2.css"));

        // Find the @media (max-width: 767.98px) block
        var mediaIndex = site2Css.IndexOf("@media (max-width: 767.98px)", StringComparison.Ordinal);
        Assert.True(mediaIndex >= 0, "Mobile media query block not found in site2.css");

        var mobileBlock = site2Css.Substring(mediaIndex);

        // Assert that none of the mobile character section classes declare a height
        var sectionClasses = new[] { "female-mage", "orc-assassin", "orc-hunter", "barbarian", "dwarf-monk" };
        foreach (var cls in sectionClasses)
        {
            var match = Regex.Match(mobileBlock, $@"\.{cls}\s*\{{([^}}]*)\}}");
            if (match.Success)
            {
                var declarations = match.Groups[1].Value;
                Assert.DoesNotMatch(@"(?<!min-|max-)height:\s*\d+px", declarations);
            }
        }
    }

    [Fact]
    public void LayoutStability_HasOverflowClipAndFluidCarouselIndicators()
    {
        var cssDir = GetWwwRootPath();
        var site2Css = File.ReadAllText(Path.Combine(cssDir, "site2.css"));
        var carouselCss = File.ReadAllText(Path.Combine(cssDir, "carousel.css"));

        // Overflow clip on body
        Assert.Contains("overflow-x: clip;", site2Css);

        // Fluid clamp indicators in carousel.css
        Assert.Contains("clamp(10px, 3vw, 30px)", carouselCss);
    }

    [Fact]
    public void CompiledAssets_ExistAndContainNoForbiddenFiles()
    {
        var cssDir = GetWwwRootPath();

        // Minified and standard files must exist and be non-empty
        var site2Css = new FileInfo(Path.Combine(cssDir, "site2.css"));
        var site2MinCss = new FileInfo(Path.Combine(cssDir, "site2.min.css"));
        var carouselCss = new FileInfo(Path.Combine(cssDir, "carousel.css"));
        var carouselMinCss = new FileInfo(Path.Combine(cssDir, "carousel.min.css"));

        Assert.True(site2Css.Exists && site2Css.Length > 0, "site2.css is missing or empty");
        Assert.True(site2MinCss.Exists && site2MinCss.Length > 0, "site2.min.css is missing or empty");
        Assert.True(carouselCss.Exists && carouselCss.Length > 0, "carousel.css is missing or empty");
        Assert.True(carouselMinCss.Exists && carouselMinCss.Length > 0, "carousel.min.css is missing or empty");

        // Forbidden files (.map and .gz) must NEVER exist in wwwroot/css/
        var mapFiles = Directory.GetFiles(cssDir, "*.map", SearchOption.AllDirectories);
        var gzFiles = Directory.GetFiles(cssDir, "*.gz", SearchOption.AllDirectories);

        Assert.Empty(mapFiles);
        Assert.Empty(gzFiles);
    }

    [Fact]
    public void Carousel_ContainsVideoPosterAndModalAccessibilityStyles()
    {
        var cssDir = GetWwwRootPath();
        var carouselCss = File.ReadAllText(Path.Combine(cssDir, "carousel.css"));

        // Video poster and play button rules
        Assert.Contains(".video-poster-container", carouselCss);
        Assert.Contains(".video-poster-img", carouselCss);
        Assert.Contains(".video-play-btn", carouselCss);
        Assert.Contains(".video-play-bg", carouselCss);
        Assert.Contains(".modal-player-slot", carouselCss);

        // Modal close button styling
        Assert.Contains(".btn-close", carouselCss);
    }

    [Fact]
    public void Carousel_ThematicModalBackgroundAndMediaElevation()
    {
        var cssDir = GetWwwRootPath();
        var carouselCss = ReadCss(cssDir, "carousel.css");

        // Thematic modal background graphics and vignette
        Assert.Contains("OrcHunter-shaded-flipped-w1920.webp", carouselCss);
        Assert.Contains("radial-gradient", carouselCss);

        // Modal media elevation shadows
        Assert.Contains("box-shadow: 0 12px 36px rgba(0, 0, 0, 0.85);", carouselCss);

        // Modal dialog has 100dvh and transparent background
        Assert.Contains(".modal-dialog {\n  margin: 0;\n  width: 100vw;\n  max-width: 100vw;\n  height: 100%;\n  height: 100dvh;\n  background: transparent;\n}", carouselCss);
    }

    [Fact]
    public void Carousel_CenteredControlsAndModernWebStandards()
    {
        var cssDir = GetWwwRootPath();
        var carouselCss = File.ReadAllText(Path.Combine(cssDir, "carousel.css"));

        // Containment & dynamic viewport
        Assert.Contains("contain: layout paint;", carouselCss);
        Assert.Contains("100dvh", carouselCss);

        // Horizontally centered controls and vertically centered indicators
        Assert.Contains(".main-carousel-control-prev", carouselCss);
        Assert.Contains(".main-carousel-control-next", carouselCss);
        Assert.Contains("border-top: 10px solid transparent;", carouselCss);
        Assert.Contains("background-clip: padding-box;", carouselCss);

        // Aspect-ratio 16:9 lock and dynamic auto height for exact uniform padding
        Assert.Contains("aspect-ratio: 16/9;", carouselCss);
        Assert.Contains("height: auto;", carouselCss);

        // Reduced motion and focus-visible
        Assert.Contains("@media (prefers-reduced-motion: reduce)", carouselCss);
        Assert.Contains(":focus-visible", carouselCss);
    }

    [Fact]
    public void ResourceHubAndModernTypography_StylesExistInCompiledCss()
    {
        var cssDir = GetWwwRootPath();
        var site2Css = File.ReadAllText(Path.Combine(cssDir, "site2.css"));

        // Resource hub card grid
        Assert.Contains(".resource-grid", site2Css);
        Assert.Contains(".resource-card", site2Css);
        Assert.Contains("grid-template-areas", site2Css);
        Assert.Contains("max-inline-size: 44rem", site2Css);

        // Keyboard focus, balanced headings and reduced motion
        Assert.Contains(":focus-visible", site2Css);
        Assert.Contains("text-wrap: balance", site2Css);
        Assert.Contains("prefers-reduced-motion", site2Css);

        // Legacy link-list styling is gone
        Assert.DoesNotContain(".important-link", site2Css);
    }

    [Fact]
    public void DarkFantasyDesignSystemAndAccessibilityStylesExist()
    {
        var cssDir = GetWwwRootPath();
        var site2Css = File.ReadAllText(Path.Combine(cssDir, "site2.css"));
        var carouselCss = File.ReadAllText(Path.Combine(cssDir, "carousel.css"));

        // Dark-fantasy design tokens, color-scheme, and sans-serif font stack
        Assert.Contains("color-scheme: dark;", site2Css);
        Assert.Contains("--gh-header-bg:", site2Css);
        Assert.Contains("--gh-card-bg:", site2Css);
        Assert.Contains("--gh-font-sans:", site2Css);
        Assert.DoesNotContain("\"Lato\", serif", site2Css);

        // Sticky header, skip link, 2x2 subgrid download panel, intro/feature cards, and footer
        Assert.Contains(".skip-link", site2Css);
        Assert.Contains(".site-header", site2Css);
        Assert.Contains(".nav-cta", site2Css);
        Assert.Contains(".download-panel", site2Css);
        Assert.Contains(".download-grid", site2Css);
        Assert.Contains("subgrid", site2Css);
        Assert.Contains(".download-platform", site2Css);
        Assert.Contains(".intro-card", site2Css);
        Assert.Contains(".feature-card", site2Css);
        Assert.Contains(".feature-list", site2Css);
        Assert.Contains(".site-footer", site2Css);

        // High Contrast / Forced Colors Mode support
        Assert.Contains("@media (forced-colors: active)", site2Css);
        Assert.Contains("@media (forced-colors: active)", carouselCss);
    }
}

