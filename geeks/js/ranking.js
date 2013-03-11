var selected, rankButton, rank = null;
var rankHtml = "<div>";
var rankButton = "<span class='rank badge'>{x}</span>";
for (var i = 0; i < 10; i++) {
    rankHtml += rankButton.replace("{x}",i+1);
}
rankHtml += "</div>";

function enablePopovers(action, tag, badgeSelector) {
    $(tag + ".rank-friend").popover({
        content: rankHtml,
        placement: "top",
        trigger: "manual",
        html: true,
        title: ''
    }).unbind("click").click(function() {
        rankButton = $(this);
        selected = rankButton.popover("show").attr('data-userId');
        $("span.rank").mouseover(function() {
            var index = $(this).index();
            $(this).parent().children().each(function(i) {
                var icon = $(this).removeClass("badge-warning");
                if(i < index + 1) icon.addClass("badge-warning");
            });
        }).click(function() {
            rank = ($(this).index() + 1);
            $.post(action, {
                __RequestVerificationToken: $("input[name='__RequestVerificationToken']").val(),
                id: selected,
                rating: rank
            }, function(data) {
                $(tag + ".rank-friend").popover("hide");
                if(badgeSelector)
                    $(badgeSelector, rankButton).text(rank).addClass("badge-warning");
                else {
                    $(rankButton).text(rank).addClass("badge-warning");
                }
            });
        });
    });
}