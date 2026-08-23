# GnollHack Marketing Website Guidelines for AI Agents

Welcome to the **GnollHack Marketing Website** repository. When working on this codebase, adhere to the following project-specific rules and conventions:

## 1. Architecture & Tech Stack
- **Framework**: ASP.NET Core 10.0 Razor Pages (`net10.0`).
- **Client Stack**: Plain JavaScript (ES6+) with **jQuery 3.7.1** and Bootstrap 5.3.3. Do not introduce TypeScript or large frontend frameworks unless explicitly requested.
- **Styling**: SCSS located in `wwwroot/css/` compiled via `compilerconfig.json`. Do not use `@import` for external fonts in SCSS; use `<link rel="preconnect">` in `_Layout.cshtml`.

## 2. Azure Front Door (AFD) Caching & Middleware Pipeline
- The site runs behind Azure Front Door.
- Header middleware in `GnollHackMarketingWebsite/Program.cs` MUST be placed **before** `app.UseRouting()`, `app.MapStaticAssets()`, and `app.MapRazorPages()`.
- Set `Cache-Control: public,max-age=31536000,immutable` for `/css`, `/js`, `/img`, `/lib`, `/favicon`, `site.webmanifest`, `robots.txt`, `sitemap.xml`.
- Set `Cache-Control: no-cache` for the root `/` and dynamic Razor Pages.

## 3. SEO & Reverse Proxy Rules
- Always resolve `BaseUrl` from `Configuration["BaseUrl"]` (default: `https://gnollhack.com`) for canonical and OpenGraph tags to avoid reverse proxy host distortion.
- Preserve Schema.org `VideoGame` structured data in `_Layout.cshtml`.
- Maintain `robots.txt` and `sitemap.xml` in `wwwroot/`.

## 4. Publishing & FTP Footprint Optimization
- Publish Profile: `GnollHackMarketingWebsite/Properties/PublishProfiles/FolderProfile.pubxml` (targets `net10.0`, `win-x64`, `SelfContained=false`).
- NEVER include `.map` source maps, `.scss` source files, `compilerconfig.json`, or temporary scripts in publish outputs.

## 5. App Store Badges & Responsive Breakpoints
- Badge image assets are borderless (`*-noborders.webp`); all borders, corner radii, and hover glows are styled via SCSS (`.shiny-borders`).
- Badges must preserve aspect ratios: desktop (`>= 768px`) enforces uniform height with natural widths; mobile (`< 768px`) enforces uniform width with natural heights.
- Always use standard Bootstrap 5 width breakpoints (`$breakpoint-md: 768px`, `@media (min-width: 768px)` / `@media (max-width: 767.98px)`) instead of `orientation: landscape/portrait` queries.
