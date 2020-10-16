# Tabs

Tabs are Teams-aware webpages embedded in Microsoft Teams. Personal tabs are scoped to a single user. They can be pinned to the left navigation bar for easy access.

## Prerequisites
**Development account**

Ensure you have access to a Teams account with the appropriate permissions to install an app. If this is your first time developing a Teams app, go through each of the steps in the [Prepare your Office 365 tenant](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/build-and-test/prepare-your-o365-tenant) page.

**Dependencies**
-  [NodeJS](https://nodejs.org/en/)
-  [yarn](https://classic.yarnpkg.com/en/docs/getting-started)
-  [ngrok](https://ngrok.com/) or equivalent tunneling solution

**Configure Ngrok**

Your app will be run from a localhost server. You will need to setup Ngrok in order to tunnel from the Teams client to localhost. 

## Build and Run

In the project directory, you can run:

### `yarn install`
Installs the required packages to run the tab.

### `yarn start`

Runs the app in the development mode.

Open [http://localhost:3000/personal](http://localhost:3000/personal) to view it in the browser. The page will reload if you make edits. You will also see any lint errors in the console.

### `yarn build`
Build the deployment package to host in your production environment.

### `ngrok http -host-header=rewrite 3000`
Run ngrok so there is a tunnel from the Internet to localhost:3000.

#### Update development.env
Update development.env in the *.publish* folder as follows:
* hostname=*somesubdomain*.ngrok.io // somesubdomain should be the subdomain in the forwarding URL provided by ngrok. 

## Deploy to Teams
Upload the `development.zip` from the *.publish* folder to Teams (in the Apps view click "Upload a custom app")
