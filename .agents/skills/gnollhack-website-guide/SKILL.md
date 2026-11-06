---
name: gnollhack-website-guide
description: >-
  Comprehensive guide and architecture reference for the GnollHack Marketing Website.
  Use this skill whenever developing, building, styling, configuring caching, or publishing
  the GnollHack marketing website.
---

# GnollHack Marketing Website Guide

This skill provides essential guidelines, architectural context, Azure Front Door (AFD) caching rules, and build/publish instructions for the **GnollHackMarketingWebsite** codebase.

---

## 1. Project Overview & Architecture

- **Platform**: ASP.NET Core 10.0 (Razor Pages) targeting `net10.0`.
- **Primary Pages**:
  - `Pages/Index.cshtml`: Main landing page with a live-text hero (separate art, crest, wordmark, slogan, buttons, and platform line), download badges, roguelike description, video/screenshot carousel, features breakdown, and community links.
  - `Pages/Index.cshtml.cs`: Supplies media items (`MediaItems`) to `_Carousel.cshtml`.
  - `Pages/Shared/_Layout.cshtml`: Global layout including SEO metadata, self-hosted fonts stylesheet, theme color, OpenGraph, JSON-LD Schema (`VideoGame`), cookie consent dialog, and script bundles.
  - `Pages/_Carousel.cshtml`: Partial for the synchronized main thumbnail carousel and fullscreen modal carousel with YouTube iframe API support.
- **Client Assets**:
  - CSS: `wwwroot/css/site2.scss` -> `site2.css` / `site2.min.css`, `carousel.scss` -> `carousel.css` / `carousel.min.css`, `fonts.scss` -> `fonts.css` / `fonts.min.css`.
  - JavaScript: Vanilla ES6+ with **jQuery 3.7.1** (`wwwroot/js/site.js`, `wwwroot/js/carousel.js`).
  - Framework: Bootstrap 5.3.3 (`wwwroot/lib/bootstrap/5.3.3/`).

---

## 2. Azure Front Door (AFD) Caching Architecture

The marketing website sits behind **Azure Front Door (AFD)** as a CDN / Edge reverse proxy. The caching rules configured in `GnollHackMarketingWebsite/Program.cs` MUST adhere to the following:

1. **Static Assets (`/css`, `/js`, `/img`, `/fonts`, `/lib`, `/favicon`, `site.webmanifest`, `robots.txt`, `sitemap.xml`)**:
   - Header: `Cache-Control: public,max-age=31536000,immutable`
   - Purpose: AFD and client browsers cache static assets for 1 year. Static assets leverage ASP.NET Core asset versioning (`asp-append-version="true"`) for automatic cache-busting on changes.
2. **Root & Dynamic Pages (`GET /`, `GET /Error`)**:
   - Header: `Cache-Control: no-cache`
   - Purpose: Ensures AFD always fetches the dynamic root page from the origin so game announcements, new releases, and live changes are immediately served to visitors.
3. **Pipeline Ordering in `Program.cs`**:
   - Custom header middleware MUST be placed **before** `app.UseRouting()`, `app.MapStaticAssets()`, and `app.MapRazorPages()`.
   - Use `context.Response.OnStarting(...)` to inject caching and security headers before response transmission begins.

---

## 3. SEO & Reverse Proxy Best Practices

- **Base URL Resolution**: Because the origin runs behind reverse proxies (Azure Front Door / App Service), `HttpContext.Request.Host` can resolve to internal hostnames. Always configure and resolve `"BaseUrl": "https://gnollhack.com"` from `appsettings.json` for canonical URLs (`<link rel="canonical">`) and OpenGraph/Twitter card image links (`<meta property="og:image">`).
- **Structured Data**: The site embeds Schema.org `VideoGame` / `SoftwareApplication` JSON-LD in `_Layout.cshtml` for rich game snippets in Google and search engines.
- **Share Image**: `wwwroot/img/gnollhack-open-graph-v2.jpg` (1200 &times; 630 px) is used by `og:image`, `twitter:image`, and the JSON-LD `image`. It is composed from the hero art, crest, and live-text styling, then encoded once from a lossless master with Pillow: quality 85, 4:4:4 chroma, optimized, progressive, no metadata, at most 150 KB. Never re-save the JPEG itself. A changed image needs a new file name, because `/img` files are cached for a year.
- **Performance (LCP / CLS)**:
  - The hero art is an `<img>` in a responsive `<picture>` with `<source media="...">` breakpoints, `srcset` widths, and `fetchpriority="high"`, never a CSS background. The `h1` is live text inside the hero. Use `@Url.Content` for `srcset` URLs, because `~/` resolves only at the start of the attribute.
  - Fonts are self-hosted from `wwwroot/fonts/` and declared in `wwwroot/css/fonts.scss`, so no third-party host is contacted before a consent choice.
  - Below-the-fold badge and character images use `loading="lazy"` and explicit dimensions.

---

## 4. SCSS Compiling Strategy, Publishing & FTP Footprint

The project enforces a strict SCSS compilation strategy across three distinct workflows to maintain a minimal FTP footprint:

1. **When a Developer Edits SCSS by Hand**:
   - The **Web Compiler 2022+** Visual Studio extension automatically compiles `.scss` files on save.
   - Using `compilerconfig.json`, it generates both `.css` and `.min.css` directly in `wwwroot/css/`. Source maps (`.map`) and gzip files are explicitly disabled to keep the workspace clean.
2. **When an AI Agent Edits SCSS**:
   - Because Visual Studio extensions do not run in agent environments, AI agents MUST manually compile modified SCSS using Dart Sass via the CLI.
   - Run the following commands to generate both standard and minified CSS without source maps:
     ```powershell
     # For site2.scss
     npx sass GnollHackMarketingWebsite/wwwroot/css/site2.scss GnollHackMarketingWebsite/wwwroot/css/site2.css --no-source-map
     npx sass GnollHackMarketingWebsite/wwwroot/css/site2.scss GnollHackMarketingWebsite/wwwroot/css/site2.min.css --style=compressed --no-source-map

     # For carousel.scss
     npx sass GnollHackMarketingWebsite/wwwroot/css/carousel.scss GnollHackMarketingWebsite/wwwroot/css/carousel.css --no-source-map
     npx sass GnollHackMarketingWebsite/wwwroot/css/carousel.scss GnollHackMarketingWebsite/wwwroot/css/carousel.min.css --style=compressed --no-source-map
     ```
3. **When the Project is Built or Published**:
   - The pre-compiled `.min.css` files are served by ASP.NET Core as static web assets.
   - Build command:
     ```powershell
     dotnet build GnollHackMarketingWebsite.slnx
     ```
   - Publish command:
     ```powershell
     dotnet publish GnollHackMarketingWebsite\GnollHackMarketingWebsite.csproj /p:PublishProfile=FolderProfile
     ```
   - Target: Framework-dependent `win-x64` (`<TargetFramework>net10.0</TargetFramework>`, `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`, `<SelfContained>false</SelfContained>`).
   - The `.csproj` explicitly excludes all `.scss`, `.map`, `compilerconfig.json`, and `.defaults` files from publish output (`CopyToPublishDirectory="Never"`).
   - NEVER include `.map` source maps, `.scss` source files, `compilerconfig.json`, `compilerconfig.*`, or temporary scripts in publish outputs.

---

## 5. Image Assets, Badges & Responsive Aspect-Ratio Preservation

### Mandatory Rule for Image Sizing & Aspect Ratios
- **Always Resolve via SCSS / HTML**: Whenever an image is stretched out of its original aspect ratio, you need to modify SCSS (and/or HTML dimensions) to make its aspect ratio correct. For this, measure the intrinsic width and height of the image (e.g. by inspecting image headers).
- **Never Modify Image Files**: Never edit, crop, re-encode, or alter the image files themselves to resolve aspect ratio issues.

### A. App Store Badges (Borderless Assets + SCSS Borders)
Badge image assets in `wwwroot/img/` are borderless (`*-noborders.webp`); all borders, corner radii, and hover glows are styled exclusively via SCSS (`.shiny-borders`).

| Badge Asset | Source Dimensions | Aspect Ratio (W/H) | SCSS Class | Border Styling | Desktop (`>= 768px`) | Mobile (`< 768px`) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `google-play-badge-noborders.webp` | 556 &times; 160 px | **3.475** | `.google-play` | `2px solid #a6a6a6`, `border-radius: 12px` | Uniform height, natural width | Uniform width, natural height |
| `app-store-badge-h200-noborders.webp` | 617 &times; 200 px | **3.085** | `.apple-app-store` | `2px solid #b2b4b6`, `border-radius: 12px` | Uniform height, natural width | Uniform width, natural height |
| `steambadge-noborders.webp` | 556 &times; 160 px | **3.475** | `.steam` | `2px solid #a8a8a8`, `border-radius: 12px` | Uniform height, natural width | Uniform width, natural height |
| `GitHubDownloadBadge-NoBorders-h200-v2.webp` | 559 &times; 200 px | **2.795** | `.github-releases` | `2px solid #a6a6a6`, `border-radius: 12px` | Uniform height, natural width | Uniform width, natural height |

- **Badge Hover Effect**: `&:hover { box-shadow: 0px 0px 12px #ddf; }`
- **Desktop (`>= 768px`)**: Badges share identical height (`height: min(max(view-width(6), 50px), 80px)`), with `width: auto; object-fit: contain` so widths naturally scale to maintain exact aspect ratios.
- **Mobile (`< 768px`)**: Badges share identical width (`width: min(view-width(80), 280px)`), with `height: auto; object-fit: contain` so heights adjust proportionally.
- **Breakpoints**: Always use standard Bootstrap 5 width breakpoints (`$breakpoint-md: 768px`, `@media (min-width: 768px)` and `@media (max-width: 767.98px)`) rather than brittle `orientation` media queries.

### B. Feature & Gameplay Icons (1x Density & Scaled Sprites)

| Icon Asset | Source Dimensions | Aspect Ratio (W:H) | Display Density | Target Display Size | Usage Location |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `human_wizard_female.webp` | 64 &times; 96 px | **2:3** (0.667) | 1x density | 64 &times; 96 px | Features (Challenge Your Wits) |
| `human_rogue_male.webp` | 64 &times; 96 px | **2:3** (0.667) | 1x density | 64 &times; 96 px | Features (Modernized for Today's Players) |
| `gnoll_barbarian_male.webp` | 64 &times; 96 px | **2:3** (0.667) | 1x density | 64 &times; 96 px | Features (Endless Replayability) |
| `stormbringer.webp` | 64 &times; 48 px | **4:3** (1.333) | 1x density | 64 &times; 48 px | Features (Free and Accessible) |

- **Container Rule**: `.features-text .imgContainer img` uses `max-width: 100%; height: auto;` alongside explicit HTML `width` and `height` attributes to prevent distortion and allow smooth responsive downscaling.

---

## 6. Dynamic Section Heights, Vertical Centering & Responsive Typography

To ensure complete stability across all viewport sizes (from 320px mobile to ultra-wide displays):

1. **Dynamic Section Heights**:
   - Never use fixed `height: ...px` on content-containing sections. Content wraps differently across device widths and will overflow or collide with adjacent sections if constrained.
   - Use dynamic height (`height: auto; min-height: auto;` or proportional `min-height`) with flexbox layouts.
2. **Vertical Centering**:
   - Use `display: flex; flex-direction: column; justify-content: center; align-items: center;` in `.general-section` and `.backdrop`.
   - Apply dynamic vertical padding to `.backdrop` (`padding: clamp(30px, 6vh, 50px) 20px;`) so content is always vertically centered with adequate breathing room at top and bottom.
### Typography Proportions & Viewport Scale Reference

All font sizes use `rem` so a visitor's browser font-size setting applies, and no text goes below 14 px at the default setting. Sizes rise smoothly above 1500 px and stop at 2560 px. Never set a font size on `html` or `:root`. Pixel values below assume the default 16 px setting. `--` means the size does not change in that range.

| Element | 320px – 768px | Up to 1500px | At 2560px and wider | Target Ratio vs. Body |
| :--- | :--- | :--- | :--- | :--- |
| **Caption** (`--gh-font-caption`) | -- | **`14px`** | **`16px`** | **~0.74&times;** |
| **Small** (`--gh-font-small`) | -- | **`16px`** | **`18px`** | **~0.84&times;** |
| **Body** (`--gh-font-body`) | -- | **`19px`** | **`24px`** | **1.00&times;** |
| **`h3`** | `27px` &rarr; `34px` *(fluid)* | **`34px`** | **`43px`** | **~1.79&times;** |
| **`h2`** | `38px` &rarr; `48px` *(fluid)* | **`48px`** | **`61px`** | **~2.53&times;** |
| **`h1`** | `44px` &rarr; `58px` *(fluid)* | **`58px`** | **`73px`** | **~3.05&times;** |

- **Caption** is for the footer, trademarks, platform labels, and cookie notice and buttons. **Small** is for navigation, the header button, skip link, resource descriptions and arrows, and the review citation.
- The `h1` scale applies to the error page. The hero has its own sizes in `.hero-wordmark` and `.hero-slogan`.

---

## 7. Automated CSS Specification & Responsive UI Testing

The solution includes a dedicated automated test suite in `GnollHackMarketingWebsite.Tests/` with three layers:

- **Layer 1: Static CSS Rule Tests (`CssSpecificationTests.cs`)**:
  - Runs text and regular-expression checks of the compiled CSS.
  - Validates that compiled `site2.css` and `carousel.css` adhere to the typography scale, mobile dynamic height rules, and overflow guards.
  - Validates that minified assets exist and forbidden files (`.map`, `.gz`) are not created.
- **Layer 2: Real-Browser Tests (`ResponsiveLayoutTests.cs`, `HeroTests.cs`, `CarouselTests.cs`, `ContentSectionTests.cs`, `CookieConsentTests.cs`, `ExternalRequestTests.cs`)**:
  - Run in headless Chromium through Playwright. Each class shares one server and one browser, through `TestServerFixture`, and each test opens its own browser context.
  - Validate `scrollWidth <= innerWidth` across the viewports listed in `ResponsiveLayoutTests.cs`.
  - Validate `getComputedStyle()` font-sizes at run time.
- **Layer 3: HTTP Header Tests (`ResponseHeaderTests.cs`)**:
  - Validates the cache and security headers from `Program.cs`.

Browser and header tests run against the real `Program.cs` through `WebApplicationFactory<Program>`, as `Development`. An unpublished `Production` run serves fingerprinted CSS and JS with empty bodies, probably because their compressed variants exist only in build or publish output. The HSTS and exception-handler branch of `Program.cs` is therefore untested.

**Running Tests**:
```powershell
dotnet test GnollHackMarketingWebsite.slnx
```

**After Updating `Microsoft.Playwright`**: build and then install its browser, or every browser test fails at launch:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File GnollHackMarketingWebsite.Tests\bin\Debug\net10.0\playwright.ps1 install chromium
```

---

## 8. Commits & Pushes

- **Never commit or push to this repository automatically.** Both need the developer's explicit permission, and the developer usually makes them manually.
- Finishing a task, a passing test run, or a plan's step does not grant that permission, and permission for one commit or push does not extend to the next.
- Leave changes uncommitted in the working tree for the developer to review.
