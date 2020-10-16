import React from 'react';
import '../App.css';
import { Link } from "react-router-dom";
import * as microsoftTeams from "@microsoft/teams-js";
import { Button, Flex, Provider, themes, EditIcon, ChatIcon } from '@fluentui/react-northstar';
import { GoogleApiWrapper } from 'google-maps-react';
import CurrentLocation from '../../data/map';

class Location extends React.Component {
  constructor(props) {
    super(props)

    this.state = {
      context: {},
      theme: {},
      showingInfoWindow: false,
      activeMarker: {},
      selectedPlace: {}
    }
  }

  onMarkerClick = (props, marker, e) =>
    this.setState({
      selectedPlace: props,
      activeMarker: marker,
      showingInfoWindow: true
    });

  onClose = props => {
    if (this.state.showingInfoWindow) {
      this.setState({
        showingInfoWindow: false,
        activeMarker: null
      });
    }
  };

  updateTheme = (themeStr) => {
    var theme;
    switch (themeStr) {
      case "dark":
        theme = themes.teamsDark;
        break;
      case "contrast":
        theme = themes.teamsHighContrast;
        break;
      case "default":
      default:
        theme = themes.teams;
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
    microsoftTeams.enablePrintCapability()
  }


  render() {
    return (
      <div>
        <Provider theme={this.state.theme}>
          <Flex><div style={{ height: '350px', width: '500px' }}>
            <CurrentLocation
              centerAroundCurrentLocation
              google={this.props.google}
            >
            </CurrentLocation>
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