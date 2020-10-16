window.addEventListener("DOMContentLoaded", function () {

    microsoftTeams.initialize();
    localStorage.removeItem("auth_error");

    let hashParams = getHashParameters();
    if (hashParams["error"]) {
        // Authentication/authorization failed
        localStorage.setItem("auth_error", JSON.stringify(hashParams));
        microsoftTeams.authentication.notifyFailure(hashParams["error"]);
    } else if (hashParams["access_token"]) {
        // Get the stored state parameter and compare with incoming state
        let expectedState = localStorage.getItem("auth_state");
        if (expectedState !== hashParams["state"]) {
            // State does not match, report error
            localStorage.setItem("auth_error", JSON.stringify(hashParams));
            microsoftTeams.authentication.notifyFailure("StateDoesNotMatch");
        } else {
            // Success -- return token information to the parent page
            microsoftTeams.authentication.notifySuccess({
                idToken: hashParams["id_token"],
                accessToken: hashParams["access_token"],
                tokenType: hashParams["token_type"],
                expiresIn: hashParams["expires_in"]
            });
        }
    } else {
        // Unexpected condition: hash does not contain error or access_token parameter
        localStorage.setItem("auth_error", JSON.stringify(hashParams));
        microsoftTeams.authentication.notifyFailure("UnexpectedFailure");
    }

    // Parse hash parameters into key-value pairs
    function getHashParameters() {
        let hashParams = {};
        location.hash.substr(1).split("&").forEach(function (item) {
            let s = item.split("="),
                k = s[0],
                v = s[1] && decodeURIComponent(s[1]);
            hashParams[k] = v;
        });
        return hashParams;
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
                getUserProfile(result.accessToken);
            },
            failureCallback: function (reason) {
                handleAuthError(reason);
            }
        });
    });

}, false);