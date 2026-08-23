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

    [Fact]
    public void Typography_MatchesSpecificationAcrossBreakpoints()
    {
        var cssDir = GetWwwRootPath();
        var site2Css = File.ReadAllText(Path.Combine(cssDir, "site2.css"));

        // Base font sizes
        Assert.Contains("font-size: clamp(44px, 11vw, 58px);", site2Css);
        Assert.Contains("font-size: clamp(38px, 9.5vw, 48px);", site2Css);
        Assert.Contains("font-size: clamp(27px, 7vw, 34px);", site2Css);
        Assert.Contains("font-size: clamp(18px, 4.5vw, 19px);", site2Css);

        // Large Screens / QHD (1500px - 2599px)
        Assert.Contains("@media (min-width: 1500px) and (max-width: 2599px)", site2Css);
        Assert.Contains("h1 {\n    font-size: 64px;\n  }", site2Css);
        Assert.Contains("h2 {\n    font-size: 53px;\n  }", site2Css);
        Assert.Contains("h3 {\n    font-size: 38px;\n  }", site2Css);
        Assert.Contains("body {\n    font-size: 21px;\n  }", site2Css);

        // Ultra-Wide / 4K (2600px+)
        Assert.Contains("@media (min-width: 2600px)", site2Css);
        Assert.Contains("h1 {\n    font-size: 73px;\n  }", site2Css);
        Assert.Contains("h2 {\n    font-size: 61px;\n  }", site2Css);
        Assert.Contains("h3 {\n    font-size: 43px;\n  }", site2Css);
        Assert.Contains("body {\n    font-size: 24px;\n  }", site2Css);
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
        var carouselCss = File.ReadAllText(Path.Combine(cssDir, "carousel.css"));

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

        // Reduced motion and focus-visible
        Assert.Contains("@media (prefers-reduced-motion: reduce)", carouselCss);
        Assert.Contains(":focus-visible", carouselCss);
    }
}

