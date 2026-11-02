using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class TestServerFixture : IAsyncLifetime
{
    private SiteFactory? _factory;
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;
    public string ServerAddress { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _factory = new SiteFactory();
        _factory.UseKestrel();
        _factory.StartServer();

        var server = _factory.Services.GetRequiredService<IServer>();
        ServerAddress = server.Features.Get<IServerAddressesFeature>()!.Addresses.First();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
        {
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();

        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    public async Task<(IBrowserContext Context, IPage Page)> OpenPageAsync(
        int width,
        int height,
        WaitUntilState waitUntil = WaitUntilState.DOMContentLoaded)
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height }
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(ServerAddress, new PageGotoOptions { WaitUntil = waitUntil });
        return (context, page);
    }

    private sealed class SiteFactory : WebApplicationFactory<Program>
    {
        // Compressed static assets exist only in build or publish output, so the site runs as Development
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseUrls("http://127.0.0.1:0");
        }
    }
}
