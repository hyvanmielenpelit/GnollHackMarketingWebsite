// GnollHack Marketing Website - Carousel & YouTube Player Integration

var ytApiReady = false;
var pendingPlayRequests = [];
var players = {};

// Load YouTube IFrame Player API asynchronously
var tag = document.createElement('script');
tag.src = "https://www.youtube.com/iframe_api";
var firstScriptTag = document.getElementsByTagName('script')[0];
if (firstScriptTag && firstScriptTag.parentNode) {
    firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);
}

function onYouTubeIframeAPIReady() {
    ytApiReady = true;
    while (pendingPlayRequests.length > 0) {
        var req = pendingPlayRequests.shift();
        playVideoInModal(req);
    }
}

function stopPlayers() {
    Object.keys(players).forEach(function (key) {
        var player = players[key];
        if (player && typeof player.pauseVideo === "function") {
            try {
                player.pauseVideo();
            } catch (e) {
                // Ignore API state errors during transitions
            }
        }
    });
}

function playVideoInModal(container) {
    var $container = $(container);
    if (!$container.hasClass('modalPlayerContainer')) {
        var index = $container.attr('data-index');
        var modalContainer = $('#modalPlayerContainer' + index);
        if (modalContainer.length > 0) {
            $container = modalContainer;
        } else {
            return;
        }
    }

    var videoId = $container.attr('data-video-id');
    var index = $container.attr('data-index');
    var slotId = 'modalPlayer' + index;

    if (!ytApiReady || typeof YT === "undefined" || !YT.Player) {
        pendingPlayRequests.push($container[0]);
        return;
    }

    $container.addClass('playing');

    if (players[slotId]) {
        try {
            players[slotId].playVideo();
        } catch (e) {
            // If player errored, re-create
        }
        return;
    }

    players[slotId] = new YT.Player(slotId, {
        videoId: videoId,
        playerVars: {
            'autoplay': 1,
            'playsinline': 1,
            'rel': 0
        },
        events: {
            'onReady': function (event) {
                event.target.playVideo();
                // Ensure generated iframe has accessible title attribute
                var iframe = document.getElementById(slotId);
                if (iframe && !iframe.getAttribute('title')) {
                    iframe.setAttribute('title', 'YouTube Video Player');
                }
            }
        }
    });
}

$(function () {
    var thumbnailCarouselElement = document.getElementById('carouselComponent');
    var modalCarouselElement = document.getElementById('modalCarousel');
    if (!thumbnailCarouselElement || !modalCarouselElement) return;

    var thumbnailCarousel = bootstrap.Carousel.getOrCreateInstance(thumbnailCarouselElement);
    var modalCarousel = bootstrap.Carousel.getOrCreateInstance(modalCarouselElement);
    var autoMovingModal = false;
    var autoMovingThumbnail = false;

    // Direct click / keypress on video play buttons and containers in modal
    $('#fullscreenModal').on('click', '.modalPlayerContainer', function (e) {
        playVideoInModal(this);
    });

    // Support keyboard activation (Enter / Space) on role="button" elements
    $('.main-carousel-container').on('keydown', '.video-poster-container[role="button"]', function (e) {
        if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            $(this).trigger('click');
        }
    });

    // Synchronize carousel slide changes
    thumbnailCarouselElement.addEventListener('slide.bs.carousel', function (event) {
        stopPlayers();
        if (!autoMovingThumbnail) {
            autoMovingModal = true;
            modalCarousel.to(event.to);
            autoMovingModal = false;
        }
    });

    modalCarouselElement.addEventListener('slide.bs.carousel', function (event) {
        stopPlayers();
        if (!autoMovingModal) {
            autoMovingThumbnail = true;
            thumbnailCarousel.to(event.to);
            autoMovingThumbnail = false;
        }
    });

    var modal = document.getElementById('fullscreenModal');
    if (modal) {
        modal.addEventListener('show.bs.modal', function (event) {
            stopPlayers();

            if (event.relatedTarget) {
                var $target = $(event.relatedTarget);
                var targetIndex = $target.attr('data-index');
                if (targetIndex === undefined) {
                    targetIndex = $target.closest('[data-index]').attr('data-index');
                }

                if (targetIndex !== undefined) {
                    var parsedIndex = parseInt(targetIndex, 10);
                    autoMovingThumbnail = true;
                    autoMovingModal = true;
                    modalCarousel.to(parsedIndex);
                    thumbnailCarousel.to(parsedIndex);
                    autoMovingThumbnail = false;
                    autoMovingModal = false;

                    // If clicked target is a video item, autoplay in modal
                    var videoId = $target.attr('data-video-id') || $target.closest('[data-video-id]').attr('data-video-id');
                    if (videoId) {
                        setTimeout(function () {
                            var modalContainer = document.getElementById('modalPlayerContainer' + parsedIndex);
                            if (modalContainer) {
                                playVideoInModal(modalContainer);
                            }
                        }, 250);
                    }
                }
            }
        });

        modal.addEventListener('hide.bs.modal', function () {
            stopPlayers();
            var $activeModalItem = $('#modalCarousel .carousel-item.active');
            if ($activeModalItem.length > 0) {
                var activeIndex = $activeModalItem.index();
                if (activeIndex >= 0) {
                    thumbnailCarousel.to(activeIndex);
                }
            }
        });
    }
});
