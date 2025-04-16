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
                ("image", "/img/slide1.png", "landscape"),
                ("image", "/img/slide2.png", "landscape"),
                ("image", "/img/steam_screenshot1.jpg", "landscape"),
                ("image", "/img/steam_screenshot2.jpg", "landscape"),
                ("image", "/img/steam_screenshot3.jpg", "landscape"),
                ("image", "/img/steam_screenshot4.jpg", "landscape"),
                ("image", "/img/steam_screenshot5.jpg", "landscape"),
                ("video", "2E0JERYB-rI", "landscape"),
            ];
        }
    }
}
