import React from 'react';
import '../App.css';
import { Link } from "react-router-dom";
import * as microsoftTeams from "@microsoft/teams-js";
import { Button, Flex, Provider, themes, EditIcon, ChatIcon } from '@fluentui/react-northstar';


class FeedBack extends React.Component {
  constructor(props) {
    super(props)
    this.state = {
      context: {},
      theme: {},
      isVideoLoading: true
    }

    this.videoTag = React.createRef();
    this.canvas = React.createRef();

  }
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
    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
      navigator.mediaDevices
        .getUserMedia({ video: { facingMode: "environment" } })
        .then(stream => {
          const video = this.videoTag.current;
          video.srcObject = stream;
          video.setAttribute("playsinline", true);
          video.play();
        });
    }

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

  takePicture = () => {
    const video = this.videoTag.current;
    const canvasElement = this.canvas.current;
    const canvas = canvasElement.getContext("2d");

    canvasElement.height = video.videoHeight;
    canvasElement.width = video.videoWidth;
    canvas.drawImage(
      video,
      0,
      0,
      canvasElement.width,
      canvasElement.height
    );

  }

  render() {
    return (
      <div>
        <Provider theme={this.state.theme}>
          <Flex>
            <div>
              <video ref={this.videoTag} width="400" height="400" autoPlay />
            </div>
            <button id="snap" class="" onClick={() => this.takePicture()}>Snap Photo</button>
            <canvas id="canvas" width="640" height="480" ref={this.canvas}></canvas>
          </Flex>
          <Flex>
            <div>

              <Link to="/personal">
                <Button icon={<EditIcon />} text primary content="Back" />
              </Link>

              <Link to="/Location">
                <Button icon={<ChatIcon />} text primary content="Location" />
              </Link>
            </div>
          </Flex>
        </Provider>
      </div>
    );
  }
}

export default FeedBack;