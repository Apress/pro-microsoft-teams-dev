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