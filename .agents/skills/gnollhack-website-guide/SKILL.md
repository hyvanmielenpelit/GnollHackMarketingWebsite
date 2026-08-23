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

## 4. Build, SCSS Compilation & Publishing

- **Build**:
  ```powershell
  dotnet build GnollHackMarketingWebsite.slnx
  ```
- **SCSS Compilation**:
  SCSS files in `wwwroot/css/` are compiled to `.css` and `.min.css` using `compilerconfig.json`.
- **Publishing (`win-x64` / Minimal FTP Size)**:
  - Profile: `GnollHackMarketingWebsite/Properties/PublishProfiles/FolderProfile.pubxml`
  - Target: Framework-dependent `win-x64` (`<TargetFramework>net10.0</TargetFramework>`, `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`, `<SelfContained>false</SelfContained>`).
  - Publish command:
    ```powershell
    dotnet publish GnollHackMarketingWebsite\GnollHackMarketingWebsite.csproj /p:PublishProfile=FolderProfile
    ```
  - **FTP Footprint Rule**: `.map` source maps, `.scss` source files, `compilerconfig.json`, and legacy ES5 transpilations are excluded from publish output to maintain minimal FTP upload transfer sizes.

---

## 5. App Store Badges, SCSS Borders & Responsive Breakpoints

- **Borderless Assets**: Badge images in `wwwroot/img/` are borderless (`*-noborders.webp`).
- **SCSS-Rendered Borders**: All badge borders and radii are styled via SCSS (`.shiny-borders`):
  - Google Play (`.google-play`): `border: 2px solid #a6a6a6; border-radius: 12px;`
  - Apple App Store (`.apple-app-store`): `border: 2px solid #b2b4b6; border-radius: 12px;`
  - Steam (`.steam`): `border: 2px solid #a8a8a8; border-radius: 12px;`
  - GitHub (`.github-releases`): `border: 2px solid #a6a6a6; border-radius: 12px;`
  - Hover Effect: `&:hover { box-shadow: 0px 0px 12px #ddf; }`
- **Asset Dimensions & Density**:
  - Google Play: 556 &times; 160 px (aspect ratio 3.475)
  - Apple App Store: 617 &times; 200 px (aspect ratio 3.085)
  - Steam: 556 &times; 160 px (aspect ratio 3.475)
  - GitHub Releases: 622 &times; 200 px (aspect ratio 3.110)
- **Responsive Aspect-Ratio Rules**:
  - **Desktop (`>= 768px` / `md+`)**: Badges share identical height (`height: min(max(view-width(6), 50px), 80px)`), with `width: auto; object-fit: contain` so widths naturally scale to maintain exact aspect ratios without distortion.
  - **Mobile (`< 768px` / `< md`)**: Badges share identical width (`width: min(view-width(80), 280px)`), with `height: auto; object-fit: contain` so heights adjust proportionally.
- **Breakpoints**: Always use standard Bootstrap 5 width breakpoints (`$breakpoint-md: 768px`, `@media (min-width: 768px)` and `@media (max-width: 767.98px)`) rather than brittle `@media (orientation: landscape/portrait)` queries.
