// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
"use strict";

$(function () {
    $("#consentButton").on("click", function () {
        $("#consentDiv").hide();
        localStorage.setItem("consentOKClicked", true);
    });
    if (localStorage.getItem("consentOKClicked") === null || localStorage.getItem("consentOKClicked") === false) {
        $("#consentDiv").show();
    }
});

