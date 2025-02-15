'use strict';

var tag = document.createElement('script');

tag.src = "https://www.youtube.com/iframe_api";
var firstScriptTag = document.getElementsByTagName('script')[0];
firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);

var players = new Array();

function onYouTubeIframeAPIReady() {
    $("div.thumbnailPlayer, div.modalPlayer").each(function (index) {
        var videoID = $(this).attr("data-video-id");
        var id = $(this).attr("id");

        players.push(new YT.Player(id, {
            videoId: videoID,
            playerVars: {
                'playsinline': 1
            }
        }));
    });
}

function stopPlayers() {
    players.forEach(function (player, index) {
        if (player) {
            player.stopVideo();
        }
    });
}

$(function () {
    var thumbnailCarouselElement = document.getElementById('carouselComponent');
    var modalCarouselElement = document.getElementById('modalCarousel');
    var thumbnailCarousel = bootstrap.Carousel.getOrCreateInstance(thumbnailCarouselElement);
    var modalCarousel = bootstrap.Carousel.getOrCreateInstance(modalCarouselElement);
    var autoMovingModal = false;
    var autoMovingThumbnail = false;

    thumbnailCarouselElement.addEventListener('slide.bs.carousel', function (event) {
        if (!autoMovingThumbnail) {
            autoMovingModal = true;
            if (event.direction === "right") {
                modalCarousel.prev();
            } else if (event.direction === "left") {
                modalCarousel.next();
            }
            autoMovingModal = false;
        }
        stopPlayers();
    });

    modalCarouselElement.addEventListener('slide.bs.carousel', function (event) {
        if (!autoMovingModal) {
            autoMovingThumbnail = true;
            if (event.direction === "right") {
                thumbnailCarousel.prev();
            } else if (event.direction === "left") {
                thumbnailCarousel.next();
            }
            autoMovingThumbnail = false;
        }
        stopPlayers();
    });

    var modal = document.getElementById('fullscreenModal');
    modal.addEventListener('show.bs.modal', function () {
        $("section.orc-hunter").height("100vh");
        stopPlayers();
    });
    modal.addEventListener('hide.bs.modal', function () {
        $("section.orc-hunter").height("");
        stopPlayers();
    });
});

