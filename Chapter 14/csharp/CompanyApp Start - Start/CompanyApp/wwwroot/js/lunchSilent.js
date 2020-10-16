window.addEventListener("DOMContentLoaded", function () {

    microsoftTeams.initialize();

    // ADAL.js configuration
    let config = {
        clientId: "fcf962d3-ab8f-48ee-af6c-c6770b78e63b",
        redirectUri: window.location.origin + "/lunch/authsilentend",       // This should be in the list of redirect uris for the AAD app
        cacheLocation: "localStorage",
        navigateToLoginRequestUrl: false
    };

    let upn = undefined;
    microsoftTeams.getContext(function (context) {
        upn = context.upn;
        loadData(upn);
    });

    // Loads data for the given user
    function loadData(upn) {
        // - login_hint provides the expected user name
        if (upn) {
            config.extraQueryParameters = "scope=openid&login_hint=" + encodeURIComponent(upn);
        } else {
            config.extraQueryParameters = "scope=openid";
        }

        let authContext = new AuthenticationContext(config);

        // See if there's a cached user and it matches the expected user
        let user = authContext.getCachedUser();
        if (user) {
            if (user.userName !== upn) {
                // User doesn't match, clear the cache
                authContext.clearCache();
            }
        }

        // Get the id token (which is the access token for resource = clientId)
        let token = authContext.getCachedToken(config.clientId);
        if (token) {
            authContext.acquireToken(
                "https://graph.microsoft.com",
                function (error, token) {
                    if (error || !token) {
                        console.log(error);
                        return;
                    }
                    // Use the access token
                    getUserProfile(token);
                }
            );

        } else {
            // No token, or token is expired
            authContext._renewIdToken(function (err, idToken) {
                if (err) {
                    console.log("Renewal failed: " + err);

                    // Failed to get the token silently; show the login button
                    $("#authenticate").css({ display: "" });

                    // You could attempt to launch the login popup here, but in browsers this could be blocked by
                    // a popup blocker, in which case the login attempt will fail with the reason FailedToOpenWindow.
                } else {
                    authContext.acquireToken(
                        "https://graph.microsoft.com",
                        function (error, token) {
                            if (error || !token) {
                                console.log(error);
                                // the user is going to authenticate
                                $("#authenticate").css({ display: "" });
                                return;
                            }
                            // Use the access token
                            getUserProfile(token);
                        }
                    );
                }
            });
        }
    }

    // Show error information
    function handleAuthError(reason) {
        console.log(reason);
    }

    function getUserProfile(accessToken) {
        $.ajax({
            url: "https://graph.microsoft.com/v1.0/me/",
            beforeSend: function (request) {
                request.setRequestHeader("Authorization", "Bearer " + accessToken);
            },
            success: function (profile) {
                $("#officeLocation").text(profile.officeLocation);

            },
            error: function (xhr, textStatus, errorThrown) {
                console.log("textStatus: " + textStatus + ", errorThrown:" + errorThrown);

            },
        });
    }

    document.getElementById('authenticate').addEventListener('click', function () {
        microsoftTeams.authentication.authenticate({
            url: window.location.origin + "/lunch/auth",
            width: 600,
            height: 535,
            successCallback: function (result) {
                getUserProfile(result);
            },
            failureCallback: function (reason) {
                handleAuthError(reason);
            }
        });
    });

}, false);