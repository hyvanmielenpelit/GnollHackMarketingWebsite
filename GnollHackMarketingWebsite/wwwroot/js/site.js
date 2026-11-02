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

    // Cookie Consent: Google Tag Manager loads only after an explicit Accept
    const $consentDiv = $("#consentDiv");
    if ($consentDiv.length > 0) {
        const consentStorageKey = "cookieConsentChoice";
        const gtmId = $consentDiv.attr("data-gtm-id");
        let gtmLoaded = false;
        let settingsReopened = false;

        const readConsentChoice = function () {
            try {
                // The former "consentOKClicked" key recorded implied consent, which is not honored
                localStorage.removeItem("consentOKClicked");
                return localStorage.getItem(consentStorageKey);
            } catch (e) {
                return null;
            }
        };

        const saveConsentChoice = function (choice) {
            try {
                localStorage.setItem(consentStorageKey, choice);
            } catch (e) {
                // Without storage the choice applies to the current page only
            }
        };

        const loadTagManager = function () {
            if (gtmLoaded) {
                return;
            }
            gtmLoaded = true;
            window.dataLayer = window.dataLayer || [];
            window.dataLayer.push({ "gtm.start": new Date().getTime(), event: "gtm.js" });
            const script = document.createElement("script");
            script.async = true;
            script.src = "https://www.googletagmanager.com/gtm.js?id=" + encodeURIComponent(gtmId);
            document.head.appendChild(script);
        };

        // Removes first-party Google Analytics and Google Ads cookies on this host and its parent domains
        const clearTrackingCookies = function () {
            const hostParts = window.location.hostname.split(".");
            const domainAttributes = [""];
            for (let i = 0; i < hostParts.length - 1; i++) {
                domainAttributes.push("; domain=." + hostParts.slice(i).join("."));
            }
            document.cookie.split(";").forEach(cookie => {
                const name = cookie.split("=")[0].trim();
                if (/^_(ga|gid|gat|gcl|gac)(_|$)/.test(name)) {
                    domainAttributes.forEach(domainAttribute => {
                        document.cookie = name + "=; Max-Age=0; path=/" + domainAttribute;
                    });
                }
            });
        };

        const storedChoice = readConsentChoice();
        if (storedChoice === "accepted") {
            loadTagManager();
        } else {
            clearTrackingCookies();
            if (storedChoice !== "declined") {
                $consentDiv.show();
            }
        }

        $consentDiv.on("click", "button[data-consent-choice]", function () {
            const choice = $(this).attr("data-consent-choice");
            saveConsentChoice(choice);
            $consentDiv.hide();
            if (choice === "accepted") {
                loadTagManager();
            } else {
                clearTrackingCookies();
                // Tags already running on this page stop only when it is reloaded
                if (gtmLoaded) {
                    window.location.reload();
                    return;
                }
            }
            if (settingsReopened) {
                settingsReopened = false;
                $("#cookieSettingsButton").trigger("focus");
            }
        });

        $("#cookieSettingsButton").on("click", function () {
            settingsReopened = true;
            $consentDiv.show();
            $("#consentAcceptButton").trigger("focus");
        });
    }

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
