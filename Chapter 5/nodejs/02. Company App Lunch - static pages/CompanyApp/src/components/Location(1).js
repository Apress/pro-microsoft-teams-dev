import React from 'react';
import './App.css';
import { Link } from "react-router-dom";
import * as microsoftTeams from "@microsoft/teams-js";
import { Button, Flex, Provider, themes, EditIcon, ChatIcon } from '@fluentui/react-northstar';
import { GoogleApiWrapper } from 'google-maps-react';
import MapShower from '../data/map';

class Location extends React.Component {
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



  componentDidMount() {
    // Get the user context from Teams and set it in the state
    microsoftTeams.getContext((context, error) => {
      this.setState({
        context: context
      });
    });
    this.updateTheme(this.getQueryVariable('theme'));
    microsoftTeams.registerOnThemeChangeHandler(this.updateTheme);
  }


  render() {
    return (
      <div>
        <Provider theme={this.state.theme}>
          <Flex><div style={{ height: '350px', width: '500px' }}>
            <MapShower
              centerAroundCurrentLocation
              google={this.props.google}
            >
            </MapShower>
          </div>
          </Flex>
          <Flex>
            <div>
              <Link to="/personal">
                <Button icon={<EditIcon />} text primary content="Back" />
              </Link>
              <Link to="/Feedback">
                <Button icon={<ChatIcon />} text primary content="Give Feedback" />
              </Link>
            </div>
          </Flex>
        </Provider>
      </div>
    );
  }
}

export default GoogleApiWrapper({
  apiKey: 'AIzaSyAiiFfFXB_JgBSzs10jtVrqbjWQ_3m_eCQ'
})(Location);