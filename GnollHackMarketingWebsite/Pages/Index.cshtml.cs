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
                ("video", "2E0JERYB-rI", "landscape"), // Use embed URL for YouTube
                ("image", "/img/slide1.png", "landscape"),
                ("image", "/img/slide2.png", "landscape"),
                ("image", "/img/slide3.png", "landscape"),
                ("video", "kTEjgzAQ2YQ", "portrait") // Use portrait for YouTube Shorts
            ];
        }
    }
}
