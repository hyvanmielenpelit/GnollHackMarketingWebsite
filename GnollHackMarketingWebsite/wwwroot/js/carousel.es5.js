'use strict';

$(function () {
    var thumbnailCarousel = document.getElementById('carouselComponent');
    var modalCarouselElement = document.getElementById('modalCarousel');
    var modalCarousel = bootstrap.Carousel.getOrCreateInstance(modalCarouselElement);

    thumbnailCarousel.addEventListener('slide.bs.carousel', function (event) {
        if (event.direction === "right") {
            modalCarousel.prev();
        } else if (event.direction === "left") {
            modalCarousel.next();
        }
    });

    var modal = document.getElementById('fullscreenModal');
    modal.addEventListener('show.bs.modal', function () {
        $("section.orc-hunter").height("100vh");
    });
    modal.addEventListener('hide.bs.modal', function () {
        $("section.orc-hunter").height("");
    });
});

