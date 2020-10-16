import React from 'react';
import '../App.css';
import * as microsoftTeams from "@microsoft/teams-js";
import {
    Flex,
    Text,
    Card,
    Provider,
    Grid,
    cardsContainerBehavior,
} from '@fluentui/react-northstar';
import news from "../../data/news.json"

class NewsTab extends React.Component {
    constructor(props) {
        super(props)
        this.state = {
            context: {},
            location: ""
        }
    }

    componentDidMount() {
        // Get the user context from Teams and set it in the state
        microsoftTeams.getContext((context, error) => {
            this.setState({
                context: context
            });
        });
        this.updateLocation(this.getQueryVariable('location'));
    }

    updateLocation = (location) => {
        this.setState({ location: location });
    }

    getQueryVariable = (variable) => {
        const query = window.location.search.substring(1);
        const vars = query.split("&");
        for (const varPairs of vars) {
            const pair = varPairs.split("=");
            if (decodeURIComponent(pair[0]) === variable) {
                return decodeURIComponent(pair[1]);
            }
        }
        return undefined;
    }


    render() {
        let loc = this.state.location;
        return (
            <Provider>
                <h1>Welcome to the news of the {this.state.location} office</h1>
                <Grid accessibility={cardsContainerBehavior} columns="2">
                    {news.map(function (item) {
                        if (item.location === loc) {
                            return <Card
                                key={item.id}
                                aria-roledescription="user card">
                                <Card.Header>
                                    <Text content={`${item.title}`} weight="bold" />
                                </Card.Header>
                                <Card.Body>
                                    <Flex column gap="gap.small">
                                        <Text content={item.contents} />
                                    </Flex>
                                </Card.Body>
                            </Card>
                        }
                    })}
                </Grid></Provider>
        );
    }
}
export default NewsTab;