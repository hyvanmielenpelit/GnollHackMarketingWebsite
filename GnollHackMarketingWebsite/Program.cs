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

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Use((context, next) =>
{
    context.Response.OnStarting(() =>
    {
        if (context.Request.Path.StartsWithSegments("/css") ||
            context.Request.Path.StartsWithSegments("/js") ||
            context.Request.Path.StartsWithSegments("/img") ||
            context.Request.Path.StartsWithSegments("/lib"))
        {
            if (context.Response.Headers["Cache-Control"].Count > 0)
            {
                context.Response.Headers.Remove("Cache-Control");
            }
            context.Response.Headers["Cache-Control"] = "public,max-age=31536000,immutable";
        }
        else if (context.Request.Path == "/")
        {
            if (context.Response.Headers["Cache-Control"].Count > 0)
            {
                context.Response.Headers.Remove("Cache-Control");
            }
            context.Response.Headers["Cache-Control"] = "no-cache";
        }
        return Task.CompletedTask;
    });

    return next(context);
});

app.Run();
