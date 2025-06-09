// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

'use strict';

function _toConsumableArray(arr) { if (Array.isArray(arr)) { for (var i = 0, arr2 = Array(arr.length); i < arr.length; i++) arr2[i] = arr[i]; return arr2; } else { return Array.from(arr); } }

var tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
var tooltipList = [].concat(_toConsumableArray(tooltipTriggerList)).map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl, {
        fallbackPlacements: ['bottom']
    });
});

$(function () {
    $("#consentButton").on("click", function () {
        $("#consentDiv").hide();
        localStorage.setItem("consentOKClicked", true);
    });
    if (localStorage.getItem("consentOKClicked") === null || localStorage.getItem("consentOKClicked") === false) {
        $("#consentDiv").show();
    }
    $("body").on("click", function (event) {
        var currentTooltipElement = $(event.target).closest('div[data-bs-toggle="tooltip"]');
        var clickHasTooltip = currentTooltipElement.length > 0;
        if (clickHasTooltip) {
            //console.log("Clicked tooltip button: ", currentTooltipElement[0]);
        } else {
                //console.log("No button associated with click.");
            }
        tooltipList.forEach(function (tooltip) {
            if (!clickHasTooltip || currentTooltipElement[0] !== tooltip._element) {
                tooltip.hide();
                //console.log("Hiding tooltip for element:", tooltip._element);
            }
        });
    });
});

