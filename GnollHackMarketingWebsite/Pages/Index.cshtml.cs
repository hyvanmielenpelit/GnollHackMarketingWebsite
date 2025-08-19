using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GnollHackMarketingWebsite.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        public List<(string, string, string)> MediaItems { get; set; } = [];

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            //

            MediaItems =
            [
                ("video", "5Ep88IYkLhs", "landscape"),
                ("image", "/img/slide1.webp", "landscape"),
                ("image", "/img/slide2.webp", "landscape"),
                ("image", "/img/steam_screenshot1.webp", "landscape"),
                ("image", "/img/steam_screenshot2.webp", "landscape"),
                ("image", "/img/steam_screenshot3.webp", "landscape"),
                ("image", "/img/steam_screenshot4.webp", "landscape"),
                ("image", "/img/steam_screenshot5.webp", "landscape"),
                ("video", "2E0JERYB-rI", "landscape"),
            ];
        }
    }
}
