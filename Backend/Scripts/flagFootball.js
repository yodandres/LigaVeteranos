$(document).ready(function () {
    
});

$("#LeagueId").change(function () {
    $("#TeamId").empty();
    $.ajax({
        type: 'POST',
        url: Url,
        dataType: 'json',
        data: { leagueId: $("#LeagueId").val() },
        success: function (data) {            
            $.each(data, function (i, item) {
                $("#TeamId").append('<option value="'
                    + item.TeamId + '">'
                    + item.Name + '</option>');
            });
        },
        error: function (ex) {
            alert('Failed to retrieve teams.' + ex);
        }
    });

    return false;
})

    $("#LocalLeagueId").change(function () {
        $("#LocalId").empty();
        $.ajax({
            type: 'POST',
            url: Url,
            dataType: 'json',
            data: { leagueId: $("#LocalLeagueId").val() },
            success: function (data) {
                $.each(data, function (i, team) {
                    $("#LocalId").append('<option value="'
                        + team.TeamId + '">'
                        + team.Name + '</option>');
                });
            },
            error: function (ex) {
                alert('Failed to retrieve teams.' + ex);
            }
        });

        return false;

    });

    $("#VisitorLeagueId").change(function () {
        $("#VisitorId").empty();
        $.ajax({
            type: 'POST',
            url: Url,
            dataType: 'json',
            data: { leagueId: $("#VisitorLeagueId").val() },
            success: function (data) {
                $.each(data, function (i, team) {
                    $("#VisitorId").append('<option value="'
                        + team.TeamId + '">'
                        + team.Name + '</option>');
                });
            },
            error: function (ex) {
                alert('Failed to retrieve teams.' + ex);
            }
        });

        return false;
    });