const locationSaveButtonSuccess = (location) => {
    microsoftTeams.settings.setValidityState(true);
    saveTab(location);
}

let saveTab = (location) => {
    microsoftTeams.settings.registerOnSaveHandler((saveEvent) => {
        microsoftTeams.settings.setSettings({
            websiteUrl: "https://proteamsdev.ngrok.io/news",
            contentUrl: "https://proteamsdev.ngrok.io/news?location=" + location,
            entityId: "CompanyNews",
            suggestedDisplayName: "News",
            removeUrl: "https://proteamsdev.ngrok.io/news/remove"
        });
        saveEvent.notifySuccess();
    });
}
$(document).ready(function () {
    $('.btn').on('click', function (event) {

        var selectedLocationTypeVal = $('[name=Location]:checked').val();

        microsoftTeams.getContext(function (context) {

            var options = [{
                "location": selectedLocationTypeVal,
                "channelId": context.channelId,
                "groupId": context.groupId,
                "teamId": context.teamId
            }];

            $.ajax({
                url: '/SaveConfiguration',
                data: JSON.stringify({ NewsConfiguration: options }),
                contentType: 'application/json;  charset=utf-8',
                dataType: 'json',
                type: 'POST',
                cache: false,
                success: function (data) {
                    locationSaveButtonSuccess(selectedLocationTypeVal);
                },
                error: function (jqXHR, textStatus) {
                    alert('jqXHR.statusCode');
                }
            });


        });


    });
});