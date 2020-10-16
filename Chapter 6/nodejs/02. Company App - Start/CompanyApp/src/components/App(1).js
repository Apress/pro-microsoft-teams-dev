import React from 'react';
import './App.css';
import * as microsoftTeams from "@microsoft/teams-js";
import { BrowserRouter as Router, Route } from "react-router-dom";

import PersonalTab from "./StaticTab/PersonalTab";
import FeedBack from "./StaticTab/FeedBack";
import Location from "./StaticTab/Location";

import NewsTab from './ConfigurableTab/NewsTab';
import NewsTabConfig from './ConfigurableTab/NewsTabConfig';

function App() {
  // Check for the Microsoft Teams SDK object.
  if (microsoftTeams) {

    // Set app routings that don't require microsoft Teams
    // SDK functionality.  Show an error if trying to access the
    // Home page.
    if (window.parent === window.self) {
      return (
        <Router>
          <Route exact path="/personal" component={TeamsHostError} />
          <Route exact path="/news" component={TeamsHostError} />
          <Route exact path="/config" component={TeamsHostError} />
        </Router>        
      );
    }

    // Initialize the Microsoft Teams SDK
    microsoftTeams.initialize(window);

    // Display the app home page hosted in Teams
    return (
      <Router>
        <Route exact path="/personal" component={PersonalTab} />
        <Route exact path="/location" component={Location} />
        <Route exact path="/feedback" component={FeedBack} />
        <Route exact path="/news" component={NewsTab} />
        <Route exact path="/config" component={NewsTabConfig} />
      </Router>
    );
  }

  // Error when the Microsoft Teams SDK is not found
  // in the project.
  return (
    <h3>Microsoft Teams SDK not found.</h3>
  );
}

/**
 * This component displays an error message in the
 * case when a page is not being hosted within Teams.
 */
class TeamsHostError extends React.Component {
  render() {
    return (
      <div>
        <h3 className="Error">Teams client host not found.</h3>
      </div>
    );
  }
}

export default App;
