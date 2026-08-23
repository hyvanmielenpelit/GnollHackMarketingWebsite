using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Azure Front Door Caching and Security Headers Middleware
app.Use((context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;

        // Security Headers
        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

        // Azure Front Door Caching Headers
        var path = context.Request.Path;
        if (path.StartsWithSegments("/css") ||
            path.StartsWithSegments("/js") ||
            path.StartsWithSegments("/img") ||
            path.StartsWithSegments("/lib") ||
            path.StartsWithSegments("/favicon") ||
            path == "/site.webmanifest" ||
            path == "/robots.txt" ||
            path == "/sitemap.xml")
        {
            headers["Cache-Control"] = "public,max-age=31536000,immutable";
        }
        else if (path == "/" || path.StartsWithSegments("/Error"))
        {
            headers["Cache-Control"] = "no-cache";
        }
        return Task.CompletedTask;
    });

    return next(context);
});

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();

public partial class Program { }
