// GnollHack Marketing Website - Site Scripts

$(function () {
    // Initialize Bootstrap Tooltips for any opted-in elements
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl, {
        fallbackPlacements: ['bottom']
    }));

    // Auto-collapse mobile navigation menu when an in-page link is selected
    const primaryNavMenu = document.getElementById('primaryNavMenu');
    if (primaryNavMenu) {
        $('.site-header').on('click', '.site-nav-link, .nav-cta, .site-brand', function () {
            if (primaryNavMenu.classList.contains('show')) {
                bootstrap.Collapse.getOrCreateInstance(primaryNavMenu).hide();
            }
        });
    }

    // Cookie Consent Banner Handling
    if (!localStorage.getItem("consentOKClicked")) {
        $("#consentDiv").show();
    }

    $("#consentButton").on("click", function () {
        $("#consentDiv").hide();
        localStorage.setItem("consentOKClicked", "true");
    });

    // Dismiss tooltips when clicking outside
    if (tooltipList.length > 0) {
        $("body").on("click", function (event) {
            var currentTooltipElement = $(event.target).closest('[data-bs-toggle="tooltip"]');
            var clickHasTooltip = currentTooltipElement.length > 0;
            tooltipList.forEach(tooltip => {
                if (!clickHasTooltip || currentTooltipElement[0] !== tooltip._element) {
                    tooltip.hide();
                }
            });
        });
    }
});
