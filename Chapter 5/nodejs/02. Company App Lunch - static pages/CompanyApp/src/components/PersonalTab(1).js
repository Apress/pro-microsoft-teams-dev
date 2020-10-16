// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import React from 'react';
import './App.css';
import * as microsoftTeams from "@microsoft/teams-js";
import lunchOptions from "../data/lunchOptions.json";
import { Button, Flex, Grid, Segment, Provider, themes, ChatIcon, LeaveIcon } from '@fluentui/react-northstar';
import { Link } from "react-router-dom";

/**
 * The 'PersonalTab' component renders the main tab content
 * of your app.
 */
class PersonalTab extends React.Component {
  constructor(props) {
    super(props)
    this.state = {
      context: {},
      theme: {}
    }
  }

  updateTheme = (themeStr) => {
    switch (themeStr) {
      case "dark":
        var theme = themes.teamsDark;
        break;
      case "contrast":
        var theme = themes.teamsHighContrast;
        break;
      case "default":
      default:
        var theme = themes.teams;
    }
    this.setState({ theme: theme });
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
  //React lifecycle method that gets called once a component has finished mounting
  //Learn more: https://reactjs.org/docs/react-component.html#componentdidmount
  componentDidMount() {
    // Get the user context from Teams and set it in the state
    microsoftTeams.getContext((context, error) => {
      this.setState({
        context: context
      });
    });
    this.updateTheme(this.getQueryVariable('theme'));
    microsoftTeams.registerOnThemeChangeHandler(this.updateTheme);
    microsoftTeams.enablePrintCapability()
  }

  render() {

    let userName = Object.keys(this.state.context).length > 0 ? this.state.context['upn'] : "";
    let name = userName.split('@')[0];
    return (

      <Provider theme={this.state.theme}>
        <Grid columns="repeat(4, 1fr)" rows="50px 180px 60px">
          <Segment color="brand" inverted styles={{ gridColumn: 'span 4', }}>
            Welcome, {name}, These are your lunch options
           </Segment>
          <Segment content="Your lunch options" inverted styles={{ gridColumn: 'span 1', }} />
          <Segment styles={{ gridColumn: 'span 3', }}>
            <table className="table">
              <thead><tr><th>ID</th><th>Name</th></tr></thead><tbody>
                {lunchOptions.map(function (item) {
                  return <tr key={item.id}><td>{item.id}</td><td>{item.name}</td><td><Button onClick={() => orderLunch(item.name)}>Order</Button></td></tr>;
                })}
              </tbody>
            </table>
          </Segment>
          <Segment styles={{ gridColumn: 'span 4', }}>
            <Link to="/Location">
              <Button icon={<LeaveIcon />} text primary content="Our Locations" />
            </Link>
            <Link to="/Feedback">
              <Button icon={<ChatIcon />} text primary content="Give Feedback" />
            </Link>
          </Segment>
        </Grid>
      </Provider>
    );

    function orderLunch(name) {
      alert(`Thanks for ordering a ${name} sandwich.!`);
    }



  }
}
export default PersonalTab;