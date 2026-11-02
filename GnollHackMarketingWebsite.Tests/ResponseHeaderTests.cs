using System.Net;
using Xunit;

namespace GnollHackMarketingWebsite.Tests;

public class ResponseHeaderTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _server;

    public ResponseHeaderTests(TestServerFixture server)
    {
        _server = server;
    }

    [Theory]
    [InlineData("css/site2.min.css")]
    [InlineData("js/site.js")]
    [InlineData("img/gnollhack-open-graph-v2.jpg")]
    [InlineData("fonts/cinzel-latin-wght-normal.woff2")]
    [InlineData("lib/bootstrap/5.3.3/bootstrap.min.css")]
    [InlineData("favicon/favicon.ico")]
    [InlineData("site.webmanifest")]
    [InlineData("robots.txt")]
    [InlineData("sitemap.xml")]
    public async Task StaticAsset_IsCacheableForOneYear(string path)
    {
        using var response = await GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("public, max-age=31536000, immutable", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task HomePage_IsRevalidatedOnEveryRequest()
    {
        using var response = await GetAsync("");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-cache", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task HomePage_HasSecurityHeaders()
    {
        using var response = await GetAsync("");

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("SAMEORIGIN", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("strict-origin-when-cross-origin", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Equal("camera=(), microphone=(), geolocation=()", Assert.Single(response.Headers.GetValues("Permissions-Policy")));
    }

    private async Task<HttpResponseMessage> GetAsync(string path)
    {
        using var client = new HttpClient();
        return await client.GetAsync(new Uri(new Uri(_server.ServerAddress), path));
    }
}
