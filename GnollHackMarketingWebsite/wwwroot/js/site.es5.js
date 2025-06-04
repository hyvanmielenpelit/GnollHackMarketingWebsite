// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

"use strict";

function _toConsumableArray(arr) { if (Array.isArray(arr)) { for (var i = 0, arr2 = Array(arr.length); i < arr.length; i++) arr2[i] = arr[i]; return arr2; } else { return Array.from(arr); } }

var tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
var tooltipList = [].concat(_toConsumableArray(tooltipTriggerList)).map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
});

$(function () {
    $("#consentButton").on("click", function () {
        $("#consentDiv").hide();
        localStorage.setItem("consentOKClicked", true);
    });
    if (localStorage.getItem("consentOKClicked") === null || localStorage.getItem("consentOKClicked") === false) {
        $("#consentDiv").show();
    }
});

