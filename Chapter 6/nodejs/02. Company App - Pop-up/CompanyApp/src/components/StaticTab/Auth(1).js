import React from 'react';
import '../App.css';
import * as microsoftTeams from "@microsoft/teams-js";


class Auth extends React.Component {


    componentDidMount() {
        microsoftTeams.initialize(window);
        // Get the user context from Teams and set it in the state
        microsoftTeams.getContext((context, error) => {
            this.setState({
                context: context
            });
            let state = Math.random();    
            localStorage.setItem("auth_state", state);
            localStorage.removeItem("auth_error");
            // Go to the Azure AD authorization endpoint
            let queryParams = {
                client_id: "fcf962d3-ab8f-48ee-af6c-c6770b78e63b",
                response_type: "token",
                response_mode: "fragment",
                scope: "https://graph.microsoft.com/User.Read openid",
                redirect_uri: window.location.origin + "/authclose",
                nonce: "AD03FBDE-ACA5-4AC3-A41A-3E60768CB7E3",
                state: state,
                // The context object is populated by Teams; the loginHint attribute
                // is used as hinting information
                login_hint: context.upn
            };

            let authorizeEndpoint = "https://login.microsoftonline.com/" + context.tid + "/oauth2/v2.0/authorize?" + this.toQueryString(queryParams);
            window.location.assign(authorizeEndpoint);
        });

    }
    toQueryString = (queryParams) => {
        let encodedQueryParams = [];
        for (let key in queryParams) {
            encodedQueryParams.push(key + "=" + encodeURIComponent(queryParams[key]));
        }
        return encodedQueryParams.join("&");
    }


    render() {
        return (
            <div></div>
        );

    }
}
export default Auth;