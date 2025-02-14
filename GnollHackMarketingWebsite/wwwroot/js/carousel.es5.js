"use strict";

var players = new Array(10);

function onYouTubeIframeAPIReady() {
    $("div.modalPlayer").each(function (index) {
        var videoID = $(this).attr("data-video-id");
        var id = $(this).attr("id");

        players[index] = new YT.Player(id, {
            videoId: videoID,
            playerVars: {
                'playsinline': 1
            }
        });
    });
}

$(function () {
    var thumbnailCarouselElement = document.getElementById('carouselComponent');
    var modalCarouselElement = document.getElementById('modalCarousel');
    var thumbnailCarousel = bootstrap.Carousel.getOrCreateInstance(thumbnailCarouselElement);
    var modalCarousel = bootstrap.Carousel.getOrCreateInstance(modalCarouselElement);

    thumbnailCarouselElement.addEventListener('slide.bs.carousel', function (event) {
        if (event.direction === "right") {
            modalCarousel.prev();
        } else if (event.direction === "left") {
            modalCarousel.next();
        }
    });

    modalCarouselElement.addEventListener('slide.bs.carousel', function (event) {
        if (event.direction === "right") {
            thumbnailCarousel.prev();
        } else if (event.direction === "left") {
            thumbnailCarousel.next();
        }
    });

    var modal = document.getElementById('fullscreenModal');
    modal.addEventListener('show.bs.modal', function () {
        $("section.orc-hunter").height("100vh");
    });
    modal.addEventListener('hide.bs.modal', function () {
        $("section.orc-hunter").height("");
        players.forEach(function (player, index) {
            if (player) {
                player.stopVideo();
            }
        });
    });
});

