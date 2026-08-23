// GnollHack Marketing Website - Carousel & YouTube Player Integration

var tag = document.createElement('script');
tag.src = "https://www.youtube.com/iframe_api";
var firstScriptTag = document.getElementsByTagName('script')[0];
if (firstScriptTag && firstScriptTag.parentNode) {
    firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);
}

var players = [];

function onYouTubeIframeAPIReady() {
    $("div.thumbnailPlayer, div.modalPlayer").each(function () {
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
    players.forEach(function (player) {
        if (player && typeof player.stopVideo === "function") {
            player.stopVideo();
        }
    });
}

$(function () {
    const thumbnailCarouselElement = document.getElementById('carouselComponent');
    const modalCarouselElement = document.getElementById('modalCarousel');
    if (!thumbnailCarouselElement || !modalCarouselElement) return;

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
    if (modal) {
        modal.addEventListener('show.bs.modal', function () {
            $("section.orc-hunter").height("100vh");
            stopPlayers();
        });
        modal.addEventListener('hide.bs.modal', function () {
            $("section.orc-hunter").height("");
            stopPlayers();
        });
    }
});
