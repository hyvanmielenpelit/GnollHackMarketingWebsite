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

## 4. SCSS Compiling Strategy, Publishing & FTP Footprint
- **Developer Workflow**: Visual Studio's "Web Compiler 2022+" compiles `.scss` to `.css` and `.min.css` on save (configured via `compilerconfig.json` with source maps and gzip disabled).
- **AI Agent Workflow**: Agents must manually compile SCSS using Dart Sass via CLI to match Web Compiler output:
  - `npx sass GnollHackMarketingWebsite/wwwroot/css/<file>.scss GnollHackMarketingWebsite/wwwroot/css/<file>.css --no-source-map`
  - `npx sass GnollHackMarketingWebsite/wwwroot/css/<file>.scss GnollHackMarketingWebsite/wwwroot/css/<file>.min.css --style=compressed --no-source-map`
- **Publish Profile**: `FolderProfile.pubxml` (targets `net10.0`, `win-x64`, `SelfContained=false`).
- **FTP Rules**: The `.csproj` excludes `.scss`, `.map`, and `compilerconfig.*` from publish outputs. NEVER upload these files to the FTP server.

## 5. Image Assets, Badges & Responsive Aspect-Ratio Preservation
- **Aspect Ratio Rule**: Whenever an image is stretched out of its original aspect ratio, modify SCSS (and/or HTML dimensions) to make its aspect ratio correct. Measure the intrinsic width and height of the image when needed. **Never modify image files themselves** to resolve aspect ratio issues.
- **App Store Badges**:
  - Badge image assets in `wwwroot/img/` are borderless (`*-noborders.webp`); all borders, corner radii, and hover glows are styled via SCSS (`.shiny-borders`).
  - Badges must preserve aspect ratios: desktop (`>= 768px`) enforces uniform height with natural widths; mobile (`< 768px`) enforces uniform width with natural heights.
  - Standard Bootstrap 5 breakpoints (`$breakpoint-md: 768px`) must be used instead of `orientation` queries.

| Badge Asset | Dimensions | Aspect Ratio | SCSS Class | Border Styling |
| :--- | :--- | :--- | :--- | :--- |
| `google-play-badge-noborders.webp` | 556 &times; 160 px | **3.475** | `.google-play` | `2px solid #a6a6a6`, `border-radius: 12px` |
| `app-store-badge-h200-noborders.webp` | 617 &times; 200 px | **3.085** | `.apple-app-store` | `2px solid #b2b4b6`, `border-radius: 12px` |
| `steambadge-noborders.webp` | 556 &times; 160 px | **3.475** | `.steam` | `2px solid #a8a8a8`, `border-radius: 12px` |
| `GitHubDownloadBadge-NoBorders-h200.webp` | 622 &times; 200 px | **3.110** | `.github-releases` | `2px solid #a6a6a6`, `border-radius: 12px` |

- **Feature & Gameplay Icons**:
  - Maintain intrinsic aspect ratios in HTML (`width` & `height`) and CSS (`max-width: 100%; height: auto;`).

| Icon Asset | Dimensions | Aspect Ratio | Display Density | Target Display Size |
| :--- | :--- | :--- | :--- | :--- |
| `human_wizard_female.webp` | 64 &times; 96 px | **2:3** (0.667) | 1x density | 64 &times; 96 px |
| `human_rogue_male.webp` | 64 &times; 96 px | **2:3** (0.667) | 1x density | 64 &times; 96 px |
| `gnoll_barbarian_male.webp` | 64 &times; 96 px | **2:3** (0.667) | 1x density | 64 &times; 96 px |
| `stormbringer.webp` | 64 &times; 48 px | **4:3** (1.333) | 1x density | 64 &times; 48 px |
| `library.webp` | 256 &times; 256 px | **1:1** (1.000) | Scaled (2x) | 128 &times; 128 px |
| `spells.webp` | 256 &times; 256 px | **1:1** (1.000) | Scaled (2x) | 128 &times; 128 px |

## 6. Dynamic Section Heights, Centering & Fluid Typography
- **No Fixed Heights**: Never use fixed `height: ...px` on content-driven sections. Always use dynamic heights (`height: auto; min-height: auto;` or proportional `min-height`) with flexbox vertical centering.
- **Vertical Padding**: Mobile `.backdrop` must use vertical padding (`padding: clamp(30px, 6vh, 50px) 20px;`) with flex-column centering so background images stretch dynamically to fit content.
- **Fluid Typography**: Use `clamp()` for headings on mobile to prevent overflow on narrow screens (>= 320px).
- **Overflow Protection**: Keep `overflow-x: clip;` on `body` to prevent horizontal jitter.

| Element | Mobile (< 400px) | Mobile / Phablet | Tablet & Desktop | Large Screens / QHD | Ultra-Wide / 4K |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `body` | `19px` | `19px` | `19px` | `21px` | `24px` |
| `h1` | `44px` *(clamp)* | `44px` &rarr; `58px` | `58px` | `64px` | `73px` |
| `h2` | `38px` *(clamp)* | `38px` &rarr; `48px` | `48px` | `53px` | `61px` |
| `h3` | `27px` *(clamp)* | `27px` &rarr; `34px` | `34px` | `38px` | `43px` |
| `h4` | `18px` *(clamp)* | `18px` &rarr; `19px` | `19px` | `21px` | `24px` |

## 7. Automated CSS Specification & Responsive Testing
- **Test Suite**: `GnollHackMarketingWebsite.Tests/` contains xUnit AST specifications (`CssSpecificationTests.cs`) and Playwright E2E responsive tests (`ResponsiveLayoutTests.cs`).
- **Run Tests**: `dotnet test GnollHackMarketingWebsite.slnx`.

