import React from 'react';
import './App.css';
import { Link } from "react-router-dom";
import { Button, Flex, Provider, themes, EditIcon,ChatIcon } from '@fluentui/react-northstar';


class Location extends React.Component {
    constructor(props) {
        super(props)
        this.state = {
          context: {},
          theme: {}
        }
      }


    render() {
      return (
        <div>
        <Provider theme={this.state.theme}>
          <Flex>
            <div>
              <ul>
                <li>
                  <Link to="/Home">
                    <Button icon={<EditIcon />} text primary content="Our Locations" />
                  </Link>
                </li>
                <li>
                  <Link to="/Feedback">
                    <Button icon={<ChatIcon />} text primary content="Give Feedback" />
                  </Link>
                </li>
              </ul>
            </div>
          </Flex>
        </Provider>
      </div>
      );
    }
}

export default Location;