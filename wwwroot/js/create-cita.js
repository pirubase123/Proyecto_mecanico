$(document).ready(function () {
    $("form").on("submit", function (event) {
        event.preventDefault(); // Prevenir el env�o por defecto del formulario
        $.ajax({
            url: '/Principal/Cita/Create', // Endpoint Razor
            type: 'POST',
            data: $(this).serialize(), // Serializar datos del formulario
        });
    });
});
