// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const {
    TurnContext,
    MessageFactory,
    TeamsActivityHandler,
    CardFactory,
    ActionTypes
} = require('botbuilder');
const { json, text } = require('express');

class BotActivityHandler extends TeamsActivityHandler {
    constructor() {
        super();

    }

    CheckImageUrl(searchTerm) {
        try {
            if (searchTerm.ToLower().Contains("outlook")) {
                return "https://support.content.office.net/en-us/media/2fa69e49-2c73-4a25-b010-47dd834b581f.png";
            }
            else if (searchTerm.ToLower().Contains("teams")) {
                return "https://www.marksgroup.net/wp-content/uploads/2017/03/Create-Channels-1.png";
            }
            else if (searchTerm.ToLower().Contains("sharepoint")) {
                return "http://www.microsoft.com/en-us/microsoft-365/blog/wp-content/uploads/sites/2/2016/07/Modern-SharePoint-lists-are-here-1-1.png";
            }
            else if (searchTerm.ToLower().Contains("word")) {
                return "https://support.content.office.net/en-us/media/ba856efe-40fe-4079-9d8f-4adce0f7b8ea.png";
            }
            else if (searchTerm.ToLower().Contains("excel")) {
                return "https://support.content.office.net/en-us/media/9d88ddfe-2c91-4dec-b32b-976d3c951a4e.png";
            }
            else if (searchTerm.ToLower().Contains("powerpoint")) {
                return "https://support.content.office.net/en-us/media/4913092f-9221-40a9-a951-c5b36e49b314.png";
            }
            else if (searchTerm.Contains("stream")) {
                return "https://img-prod-cms-rt-microsoft-com.akamaized.net/cms/api/am/imageFileData/RWpaTX?ver=f5b2&q=90&m=2&h=768&w=1024&b=%23FFFFFFFF&aim=true";
            }
            else {
                return "https://www.microsoft.com/en-us/microsoft-365/blog/wp-content/uploads/sites/2/2014/02/OfficecomHomeCrop780.png";
            }
        }
        catch (Exception) {
            return "https://support.content.office.net/en-us/media/4d96bcbc-ed1b-41a7-a365-f71a41cab2bb.png";
        }
    }

   // Invoked when the service receives an incoming search query.
    async handleTeamsMessagingExtensionQuery(context, query) {
        const axios = require('axios');
        const querystring = require('querystring');
        const request = require("request");
        const searchQuery = query.parameters[0].value;

        var subscriptionKey = '5c00e48b0d034ecebc333d89e0234bfb';
        var customConfigId = '4fb85d73-afae-4aed-b7da-a7ff6c4ce11a';

        const response = await axios.get('https://api.cognitive.microsoft.com/bingcustomsearch/v7.0/search?' +
        'q=' + searchQuery + "&" + 'customconfig=' + customConfigId, {
            headers: {
                'Ocp-Apim-Subscription-Key': subscriptionKey
            }
        });

        const attachments = [];
        response.data.webPages.value.forEach(obj => {
            const heroCard = CardFactory.heroCard(obj.name);
            const preview = CardFactory.thumbnailCard(obj.name);
            var imageUrl = this.CheckImageUrl(obj.snippet);
            preview.content.tap = { type: 'invoke', value: { name: obj.name, snippet: obj.snippet, link: obj.url, img: imageUrl } };
            preview.images = CardFactory.images([imageUrl])
            const attachment = { ...heroCard, preview };
            attachments.push(attachment);
        });

        return {
            composeExtension: {
                type: 'result',
                attachmentLayout: 'list',
                attachments: attachments
            }
        };
    }

    // Invoked when the user selects an item from the search result list returned above.
    async handleTeamsMessagingExtensionSelectItem(context, obj) {
        const thumbnailCard = CardFactory.thumbnailCard(obj.name,
            CardFactory.images([obj.img]),
            CardFactory.actions([{
                type: ActionTypes.OpenUrl,
                title: 'Open in browser',
                value: obj.link
            }])
        );

        return {
            composeExtension: {
                type: 'result',
                attachmentLayout: 'list',
                attachments: [thumbnailCard]
            }
        };
    }
}


module.exports.BotActivityHandler = BotActivityHandler;
