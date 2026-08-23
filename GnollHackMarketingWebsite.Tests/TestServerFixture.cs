using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class TestServerFixture : IAsyncLifetime
{
    private WebApplication? _app;
    public string ServerAddress { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var contentRoot = GetContentRootPath();
        var webRoot = Path.Combine(contentRoot, "wwwroot");

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = contentRoot,
            WebRootPath = webRoot,
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        builder.Services.AddRazorPages()
            .AddApplicationPart(typeof(Program).Assembly);

        _app = builder.Build();

        _app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(webRoot)
        });
        _app.UseRouting();
        _app.MapRazorPages();

        await _app.StartAsync();

        var server = _app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        ServerAddress = addresses?.Addresses.FirstOrDefault() ?? "http://127.0.0.1:5000";
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private static string GetContentRootPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(currentDir);
        while (dir != null)
        {
            var target = Path.Combine(dir.FullName, "GnollHackMarketingWebsite");
            if (Directory.Exists(target) && File.Exists(Path.Combine(target, "Program.cs")))
            {
                return target;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate GnollHackMarketingWebsite project directory.");
    }
}
