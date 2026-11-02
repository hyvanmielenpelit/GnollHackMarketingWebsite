# GnollHack Marketing Website Guidelines for AI Agents

Welcome to the **GnollHack Marketing Website** repository. When working on this codebase, adhere to the following project-specific rules and conventions:

## 1. Architecture & Tech Stack
- **Framework**: ASP.NET Core 10.0 Razor Pages (`net10.0`).
- **Client Stack**: Plain JavaScript (ES6+) with **jQuery 3.7.1** and Bootstrap 5.3.3. Do not introduce TypeScript or large frontend frameworks unless explicitly requested.
- **Styling**: SCSS located in `wwwroot/css/` compiled via `compilerconfig.json`. Fonts are self-hosted: files live in `wwwroot/fonts/` (with their OFL licenses) and are declared in `wwwroot/css/fonts.scss`. Never load fonts from a third-party host or add `preconnect` hints for one, because that sends the visitor's IP address to the host before any consent choice. `_Layout.cshtml` preloads the Cinzel file so the hero wordmark does not shift when the font swaps in; the preload URL must stay identical to the one in `fonts.scss`.

## 2. Azure Front Door (AFD) Caching & Middleware Pipeline
- The site runs behind Azure Front Door.
- Header middleware in `GnollHackMarketingWebsite/Program.cs` MUST be placed **before** `app.UseRouting()`, `app.MapStaticAssets()`, and `app.MapRazorPages()`.
- Set `Cache-Control: public,max-age=31536000,immutable` for `/css`, `/js`, `/img`, `/fonts`, `/lib`, `/favicon`, `site.webmanifest`, `robots.txt`, `sitemap.xml`.
- Set `Cache-Control: no-cache` for the root `/` and dynamic Razor Pages.

## 3. SEO & Reverse Proxy Rules
- Always resolve `BaseUrl` from `Configuration["BaseUrl"]` (default: `https://gnollhack.com`) for canonical and OpenGraph tags to avoid reverse proxy host distortion.
- Preserve Schema.org `VideoGame` structured data in `_Layout.cshtml`.
- The share image is `wwwroot/img/gnollhack-open-graph-v2.jpg`, composed at 1200 &times; 630 px. `og:image`, `twitter:image`, and the JSON-LD `image` all point at it.
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

- **Video Posters**: Served locally as `wwwroot/img/video-poster-<YouTube ID>.jpg` (1280 &times; 720 px, **16:9**), never from `img.youtube.com`. A new video in `Pages/Index.cshtml.cs` needs its own poster file there.
- **Carousel Images**: Slide images keep `loading="lazy"` in the markup. `wwwroot/js/carousel.js` switches them to eager when the carousel nears the viewport, because hidden slides do not fetch lazy images and would otherwise be blank while sliding in.

- **Hero Art**: An `<img>` in a `<picture>`, never a CSS background, with no logo or text burnt in. WebP at quality 85. Use `@Url.Content` for `srcset` URLs, because `~/` resolves only at the start of the attribute.

| Hero Asset | Dimensions | Used At |
| :--- | :--- | :--- |
| `hero-ranger-landscape-w2560.webp` | 2560 &times; 1637 px | `>= 768px` |
| `hero-ranger-landscape-w1920.webp` | 1920 &times; 1227 px | `>= 768px`, and the `<img>` fallback |
| `hero-ranger-landscape-w1280.webp` | 1280 &times; 818 px | `>= 768px` |
| `hero-ranger-portrait-w1080.webp` | 1080 &times; 1620 px | `< 768px` |
| `hero-ranger-portrait-w750.webp` | 750 &times; 1125 px | `< 768px` |

- **Share Image JPEG**: Encode once from a lossless master with Pillow at quality 85, 4:4:4 chroma (`subsampling=0`), optimized, progressive, with no metadata and at most 150 KB. Never re-save the JPEG itself. A changed image needs a new file name, because `/img` files are cached for a year.

- **Feature & Gameplay Icons**:
  - Maintain intrinsic aspect ratios in HTML (`width` & `height`) and CSS (`max-width: 100%; height: auto;`).

| Icon Asset | Dimensions | Aspect Ratio | Display Density | Target Display Size |
| :--- | :--- | :--- | :--- | :--- |
| `human_wizard_female.webp` | 64 &times; 96 px | **2:3** (0.667) | 1x density | 64 &times; 96 px |
| `human_rogue_male.webp` | 64 &times; 96 px | **2:3** (0.667) | 1x density | 64 &times; 96 px |
| `gnoll_barbarian_male.webp` | 64 &times; 96 px | **2:3** (0.667) | 1x density | 64 &times; 96 px |
| `stormbringer.webp` | 64 &times; 48 px | **4:3** (1.333) | 1x density | 64 &times; 48 px |

## 6. Dynamic Section Heights, Centering & Fluid Typography
- **No Fixed Heights**: Never use fixed `height: ...px` on content-driven sections. Always use dynamic heights (`height: auto; min-height: auto;` or proportional `min-height`) with flexbox vertical centering.
- **Vertical Padding**: Mobile `.backdrop` must use vertical padding (`padding: clamp(30px, 6vh, 50px) 20px;`) with flex-column centering so background images stretch dynamically to fit content.
- **Fluid Typography**: Use `clamp()` for headings on mobile to prevent overflow on narrow screens (>= 320px).
- **Font Size Units**: Set every font size in `rem` so a visitor's browser font-size setting applies, combine `rem` and `vw` in fluid sizes, and never set a font size on `html` or `:root`.
- **Minimum Text Size**: No text goes below 14 px at the default browser setting.
- **Overflow Protection**: Keep `overflow-x: clip;` on `body` to prevent horizontal jitter.

Sizes assume the default 16 px browser setting and rise smoothly from 1500 px to 2560 px. `--` means the size does not change in that range.

| Tier | 320 px to 768 px | Up to 1500 px | At 2560 px and wider | Used By |
| :--- | :--- | :--- | :--- | :--- |
| Caption (`--gh-font-caption`) | -- | `14px` | `16px` | Footer, trademarks, platform labels, cookie notice and buttons |
| Small (`--gh-font-small`) | -- | `16px` | `18px` | Navigation, header button, skip link, resource descriptions and arrows, review citation |
| Body (`--gh-font-body`) | -- | `19px` | `24px` | Body, lead text, resource links, review quote |
| `h3` | `27px` &rarr; `34px` | `34px` | `43px` | |
| `h2` | `38px` &rarr; `48px` | `48px` | `61px` | |
| `h1` | `44px` &rarr; `58px` | `58px` | `73px` | Error page; the hero has its own sizes |

## 7. Automated CSS Specification & Responsive Testing
- **CSS Specifications**: `CssSpecificationTests.cs` runs text and regular-expression checks of the compiled CSS.
- **Browser Tests**: `ResponsiveLayoutTests.cs`, `HeroTests.cs`, `CarouselTests.cs`, `ContentSectionTests.cs`, `CookieConsentTests.cs`, and `ExternalRequestTests.cs` run in Playwright. Each class shares one server and one Chromium, through `TestServerFixture`, and each test opens its own browser context.
- **Header Tests**: `ResponseHeaderTests.cs` checks the cache and security headers over HTTP.
- **Test Environment**: Browser and header tests run against the real `Program.cs` through `WebApplicationFactory<Program>`, as `Development`. An unpublished `Production` run serves fingerprinted CSS and JS with empty bodies, probably because their compressed variants exist only in build or publish output. The HSTS and exception-handler branch of `Program.cs` is therefore untested.
- **Playwright Updates**: After updating `Microsoft.Playwright`, build and then install its browser, or every browser test fails at launch: `powershell -NoProfile -ExecutionPolicy Bypass -File GnollHackMarketingWebsite.Tests\bin\Debug\net10.0\playwright.ps1 install chromium`.
- **Run Tests**: `dotnet test GnollHackMarketingWebsite.slnx`.

## 8. Cookie Consent
- Non-essential third-party tags load only after an explicit Accept. `wwwroot/js/site.js` injects Google Tag Manager from `#consentDiv`'s `data-gtm-id`; never add a tag snippet or `<noscript>` tag frame to `_Layout.cshtml`.
- Accept and Decline must stay equally prominent, and the footer's Cookie Settings button must keep reopening the notice.
- If the notice's tag list changes, update the Cookie Policy wiki page too.

## 9. Commits & Pushes
- **Never commit or push to this repository automatically.** Both need the developer's explicit permission, and the developer usually makes them manually.
- Finishing a task, a passing test run, or a plan's step does not grant that permission, and permission for one commit or push does not extend to the next.
- Leave changes uncommitted in the working tree for the developer to review.
