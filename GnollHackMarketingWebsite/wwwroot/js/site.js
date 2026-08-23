// GnollHack Marketing Website - Site Scripts

$(function () {
    // Initialize Bootstrap Tooltips
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerEl => new bootstrap.Tooltip(tooltipTriggerEl, {
        fallbackPlacements: ['bottom']
    }));

    // Cookie Consent Banner Handling
    if (!localStorage.getItem("consentOKClicked")) {
        $("#consentDiv").show();
    }

    $("#consentButton").on("click", function () {
        $("#consentDiv").hide();
        localStorage.setItem("consentOKClicked", "true");
    });

    // Dismiss tooltips when clicking outside
    $("body").on("click", function (event) {
        var currentTooltipElement = $(event.target).closest('div[data-bs-toggle="tooltip"]');
        var clickHasTooltip = currentTooltipElement.length > 0;
        tooltipList.forEach(tooltip => {
            if (!clickHasTooltip || currentTooltipElement[0] !== tooltip._element) {
                tooltip.hide();
            }
        });
    });
});