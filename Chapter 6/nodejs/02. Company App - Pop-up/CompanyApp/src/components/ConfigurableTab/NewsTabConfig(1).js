import React from 'react';
import '../App.css';
import * as microsoftTeams from "@microsoft/teams-js";
import { Dropdown } from '@fluentui/react-northstar';

const inputItems = [
    'America',
    'Europe',
    'Asia'
];


class NewsTabConfig extends React.Component {
    constructor(props) {
        super(props)
        this.state = {
            context: {}
        }
    }

    componentDidMount() {
        // Get the user context from Teams and set it in the state
        microsoftTeams.getContext((context, error) => {
            this.setState({
                context: context
            });
        });
    }

    enableSave = (key) => {
        var contentUrl = "https://proteamsdev2.ngrok.io/news?location=" + key.value;
        microsoftTeams.settings.setValidityState(true);
        microsoftTeams.settings.registerOnSaveHandler((saveEvent) => {
            microsoftTeams.settings.setSettings({
                contentUrl: contentUrl,
                entityId: "CompanyNews",
                suggestedDisplayName: "News"
            });
            saveEvent.notifySuccess();
        });
    }


    render() {
        /**
         * The content url for the tab is a required value that must be set.
         * The url value is the source url for your configured tab.
         * This allows for the addition of query string parameters based on
         * the settings selected by the user.
         */


        /**
         * After verifying that the settings for your tab are correctly
         * filled in by the user you need to set the state of the dialog
         * to be valid.  This will enable the save button in the configuration
         * dialog.
         */


        return (
            <div>
                <h1>Welcome to our Company news app</h1>
                <Dropdown
                    items={inputItems}
                    placeholder="Please select your office location"
                    checkable
                    onChange={(e, selectedOption) => {
                        this.enableSave(selectedOption)
                    }}

                />
            </div>
        );
    }
}

export default NewsTabConfig;