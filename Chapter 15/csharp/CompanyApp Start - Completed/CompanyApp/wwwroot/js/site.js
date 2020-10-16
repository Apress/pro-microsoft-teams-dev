
microsoftTeams.initialize();

microsoftTeams.getContext(function (context) {
    var theme = context.theme;
    var color = "#252423";
    if (theme === "default") {
        color = "#252423";
    } else if (theme === "dark") {
        color = "#F3F2F1";
    } else if (theme === "contrast") {
        color = "#ffff01";
    }

    var h1Elements = document.getElementsByTagName("h1");

    for(var i = 0; i < h1Elements.length; i++) {
        h1Elements[i].style.color = color;
    }
});

microsoftTeams.registerOnThemeChangeHandler(function(theme) {
    var color = "#252423";
    if (theme === "default") {
        color = "#252423";
    } else if (theme === "dark") {
        color = "#F3F2F1";
    } else if (theme === "contrast") {
        color = "#ffff01";
    }

    var h1Elements = document.getElementsByTagName("h1");

    for(var i = 0; i < h1Elements.length; i++) {
        h1Elements[i].style.color = color;
    }

});