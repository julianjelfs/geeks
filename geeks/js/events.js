var selected = null;
$(function() {
    $("#nav-bar li.active").removeClass("active");
    $("#events-nav").addClass("active");
    $("a.delete-event").tooltip({
        animation: true,
        title: 'Cancel the event',
        trigger: 'hover',
        placement: 'right'
    }).click(function(e) {
        e.preventDefault();
        selected = $(this).attr('data-eventid');
        $("#delete-warning").modal();
    });
    $("a.reschedule-event").tooltip({
        animation: true,
        title: 'Reschedule the event',
        trigger: 'hover',
        placement: 'right'
    });

    $("#delete-event").click(function(e) {
        e.preventDefault();
        $.post("/Home/DeleteEvent/" + selected, {
                __RequestVerificationToken: $("input[name='__RequestVerificationToken']").val()
            }, function(data) {
                window.location.href = window.location.href;
            });
    });

});