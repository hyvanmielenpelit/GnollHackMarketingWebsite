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
  - `Pages/Index.cshtml`: Main landing page with hero picture, download badges, roguelike description, video/screenshot carousel, features breakdown, and community links.
  - `Pages/Index.cshtml.cs`: Supplies media items (`MediaItems`) to `_Carousel.cshtml`.
  - `Pages/Shared/_Layout.cshtml`: Global layout including SEO metadata, Google Fonts, theme color, OpenGraph, JSON-LD Schema (`VideoGame`), cookie consent dialog, and script bundles.
  - `Pages/_Carousel.cshtml`: Partial for the synchronized main thumbnail carousel and fullscreen modal carousel with YouTube iframe API support.
- **Client Assets**:
  - CSS: `wwwroot/css/site2.scss` -> `site2.css` / `site2.min.css`, `carousel.scss` -> `carousel.css` / `carousel.min.css`.
  - JavaScript: Vanilla ES6+ with **jQuery 3.7.1** (`wwwroot/js/site.js`, `wwwroot/js/carousel.js`).
  - Framework: Bootstrap 5.3.3 (`wwwroot/lib/bootstrap/5.3.3/`).

---

## 2. Azure Front Door (AFD) Caching Architecture

The marketing website sits behind **Azure Front Door (AFD)** as a CDN / Edge reverse proxy. The caching rules configured in `GnollHackMarketingWebsite/Program.cs` MUST adhere to the following:

1. **Static Assets (`/css`, `/js`, `/img`, `/lib`, `/favicon`, `site.webmanifest`, `robots.txt`, `sitemap.xml`)**:
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
- **Performance (LCP / CLS)**:
  - The hero image uses a responsive `<picture>` element with `<source media="...">` breakpoints and `fetchpriority="high"`.
  - Google Fonts are preconnected (`<link rel="preconnect">`) in `<head>` rather than `@import` in CSS.
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
| `GitHubDownloadBadge-NoBorders-h200.webp` | 622 &times; 200 px | **3.110** | `.github-releases` | `2px solid #a6a6a6`, `border-radius: 12px` | Uniform height, natural width | Uniform width, natural height |

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
| `library.webp` | 256 &times; 256 px | **1:1** (1.000) | Scaled (2x) | 128 &times; 128 px | Gameplay Information (mobile portrait) |
| `spells.webp` | 256 &times; 256 px | **1:1** (1.000) | Scaled (2x) | 128 &times; 128 px | Community (mobile portrait) |

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

| Element | Mobile (< 400px)<br>*(e.g., 360px)* | Mobile / Phablet<br>*(400px – 530px)* | Tablet & Standard Desktop<br>*(530px – 1499px)* | Large Screens / QHD<br>*(1500px – 2599px)* | Ultra-Wide / 4K<br>*(≥ 2600px)* | Target Ratio vs. Body |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`body`** | **`19px`** | **`19px`** | **`19px`** | **`21px`** | **`24px`** | **1.00&times;** |
| **`h1`** | **`44px`** *(clamp min)* | `44px` &rarr; `58px` *(fluid)* | **`58px`** | **`64px`** | **`73px`** | **~3.05&times;** |
| **`h2`** | **`38px`** *(clamp min)* | `38px` &rarr; `48px` *(fluid)* | **`48px`** | **`53px`** | **`61px`** | **~2.53&times;** |
| **`h3`** | **`27px`** *(clamp min)* | `27px` &rarr; `34px` *(fluid)* | **`34px`** | **`38px`** | **`43px`** | **~1.79&times;** |
| **`h4`** | **`18px`** *(clamp min)* | `18px` &rarr; `19px` *(fluid)* | **`19px`** | **`21px`** | **`24px`** | **1.00&times;** |

---

## 7. Automated CSS Specification & Responsive UI Testing

The solution includes a dedicated automated test suite in `GnollHackMarketingWebsite.Tests/` using the 3-Layer UI Testing Pyramid:

- **Layer 1: Static CSS Rule & AST Tests (`CssSpecificationTests.cs`)**:
  - Validates that compiled `site2.css` and `carousel.css` adhere to the typography scale, mobile dynamic height rules, and overflow guards.
  - Validates that minified assets exist and forbidden files (`.map`, `.gz`) are not created.
- **Layer 2: Real-Browser Computed Style & Overflow Tests (`ResponsiveLayoutTests.cs`)**:
  - Uses Playwright + `WebApplicationFactory` to spin up headless Chromium.
  - Validates `scrollWidth <= innerWidth` across canonical viewports (`320px`, `360px`, `390px`, `430px`, `768px`, `1920px`, `3840px`).
  - Validates `getComputedStyle()` font-sizes at run time.

**Running Tests**:
```powershell
dotnet test GnollHackMarketingWebsite.slnx
```
