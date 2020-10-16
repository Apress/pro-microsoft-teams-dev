# Bots/Messaging Extension

*Bots* allow users to interact with your web service through text, interactive cards, and task modules. *Messaging extensions* allow users to interact with your web service through buttons and forms in the Microsoft Teams client. They can search, or initiate actions, in an external system from the compose message area, the command box, or directly from a message.

## Prerequisites
**Development account**

Ensure you have access to a Teams account with the appropriate permissions to install an app. If this is your first time developing a Teams app, go through each of the steps in the [Prepare your Office 365 tenant](https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/build-and-test/prepare-your-o365-tenant) page.

**Dependencies**
-  [NodeJS](https://nodejs.org/en/)
-  [ngrok](https://ngrok.com/) or equivalent tunneling solution

**Configure Ngrok**

Your app will be run from a localhost server. You will need to setup Ngrok in order to tunnel from the Teams client to localhost. 

**Run Ngrok**

Run ngrok - point to port 3978

`ngrok http -host-header=rewrite 3978`

**Register your bot with Bot Framework**

  Note: You can also do this with the Manifest Editor in App Studio if you are familiar with the process. We are working on building this step into the extension in the coming sprint.

- Create [Bot Framework registration resource](https://docs.microsoft.com/en-us/azure/bot-service/bot-service-quickstart-registration) in Azure

- For the Messaging endpoint URL, use the current `https` URL you were given by running ngrok and append it with the path `/api/messages`. It should like something work `https://subdomain.ngrok.io/api/messages`.

- Ensure that you've [enabled the Teams Channel](https://docs.microsoft.com/en-us/azure/bot-service/channel-connect-teams?view=azure-bot-service-4.0)

- __*If you don't have an Azure account*__ you can use this [Bot Framework registration](https://docs.microsoft.com/en-us/microsoftteams/platform/bots/how-to/create-a-bot-for-teams#register-your-web-service-with-the-bot-framework)

- Copy your bot's ID (*BOT_ID*) and password/secret (*BOT_PASSWORD*) to be used in later steps.

## Build and run

In the project directory, you can run:

### `npm install`
Installs the required packages to run the tab.

### Set the bot ID and password in your env files

#### Update development.env
Update development.env in the *.publish* folder.

`botId0=BOT_ID`

#### Update .env
Update the .env file in your service project's root folder with your bot password.

`BotID=BOT_ID`
`BotPassword=BOT_PASSWORD`

## Run your bot at the command line:

`npm start`

## Deploy to Teams

Upload the `development.zip` from the *.publish* folder to Teams (in the Apps view click "Upload a custom app")

## Interacting with the bot
You can interact with this bot by sending it a message, or selecting a command from the command list. The bot will respond to the following strings.

 **Hello**
 
**Result:** The bot will respond to the message and mention the user.
