import React from 'react';
import '../App.css';
import * as microsoftTeams from "@microsoft/teams-js";


class AuthClose extends React.Component {

    componentDidMount() {
        // Get the user context from Teams and set it in the state
        microsoftTeams.initialize(window);
        microsoftTeams.getContext((context, error) => {
            this.setState({
                context: context
            });
            localStorage.removeItem("auth_error");

            let hashParams = this.getHashParameters();
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
        });

    }
    getHashParameters = () => {
        let hashParams = {};
        window.location.hash.substr(1).split("&").forEach(function (item) {
            let s = item.split("="),
                k = s[0],
                v = s[1] && decodeURIComponent(s[1]);
            hashParams[k] = v;
        });
        return hashParams;
    }


    render() {
        return (
            <div></div>
        );
    }
}
export default AuthClose;