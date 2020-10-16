using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CompanyApp.Data;
using CompanyApp.Dialogs;
using CompanyApp.Models;
using CompanyApp.Repository;
using Microsoft.Azure.CognitiveServices.Search.CustomSearch;
using Microsoft.Azure.CognitiveServices.Search.CustomSearch.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CompanyApp.Bots
{
    public class QABot<T> : TeamsActivityHandler
        where T : Dialog
    {
        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        private readonly IConfiguration _configuration;
        protected readonly ILogger Logger;
        private readonly TeamObjectRepository _teamrepository;
        private readonly UserObjectRepository _userrepository;
        private static readonly string CancelMsgText = "Cancelling...";


        public QABot(ConversationState conversationState, UserState userState, IConfiguration configuration, T dialog, ILogger<QABot<T>> logger, TeamObjectRepository teamRepository, UserObjectRepository userRepository)
        {
            ConversationState = conversationState;
            UserState = userState;
            _configuration = configuration;
            Dialog = dialog;
            Logger = logger;
            _teamrepository = teamRepository;
            _userrepository = userRepository;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            if (await InterruptAsync(turnContext, cancellationToken))
            {
                return;
            }
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnTeamsMembersAddedAsync(IList<TeamsChannelAccount> teamsMembersAdded, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var botId = turnContext.Activity.Recipient.Id;
            if (teamsMembersAdded.FirstOrDefault(p => p.Id == botId) != null) //check if it's the bot that's added
            {
                if (teamInfo?.Id != null) //inside team
                {
                    var team = new TeamObject()
                    {
                        PartitionKey = Constants.TeamDataPartition,
                        RowKey = teamInfo.Id,
                        Id = teamInfo.Id,
                        Name = teamInfo.Name,
                        ServiceUrl = turnContext.Activity.ServiceUrl,
                        BotId = turnContext.Activity?.Recipient?.Id,
                        BotName = turnContext.Activity?.Recipient?.Name,
                    };
                    await _teamrepository.CreateOrUpdateAsync(team);
                }
                else
                {
                    var user = new UserObject
                    {
                        PartitionKey = Constants.UserDataPartition,
                        RowKey = turnContext.Activity?.From?.AadObjectId,
                        UserId = turnContext.Activity?.From?.Id,
                        Name = turnContext.Activity?.From?.Name,
                        ConversationId = turnContext.Activity?.Conversation?.Id,
                        ServiceUrl = turnContext.Activity?.ServiceUrl
                    };

                    await _userrepository.CreateOrUpdateAsync(user);
                }
            }
        }

        protected override async Task OnTeamsMembersRemovedAsync(IList<TeamsChannelAccount> teamsMembersRemoved, TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var botId = turnContext.Activity.Recipient.Id;
            if (teamsMembersRemoved.FirstOrDefault(p => p.Id == botId) != null) //check if it's the bot that's added
            {
                if (teamInfo?.Id != null) //inside team
                {
                    var team = await _teamrepository.GetAsync(Constants.TeamDataPartition, teamInfo.Id);
                    await _teamrepository.DeleteAsync(team);
                }
                else
                {
                    var user = await _userrepository.GetAsync(Constants.UserDataPartition,
                        turnContext.Activity?.From?.AadObjectId);

                    await _userrepository.DeleteAsync(user);
                }
            }
        }


        protected override async Task OnTeamsTeamRenamedAsync(TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var team = new TeamObject()
            {
                PartitionKey = Constants.TeamDataPartition,
                RowKey = teamInfo.Id,
                Id = teamInfo.Id,
                Name = teamInfo.Name,
                ServiceUrl = turnContext.Activity.ServiceUrl,
            };
            await _teamrepository.CreateOrUpdateAsync(team);
        }

        protected override async Task OnReactionsAddedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var reaction in messageReactions)
            {
                var newReaction = $"You reacted with '{reaction.Type}' to the following message: '{turnContext.Activity.ReplyToId}'";
                var replyActivity = MessageFactory.Text(newReaction);
                var resourceResponse = await turnContext.SendActivityAsync(replyActivity, cancellationToken);
            }
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionQueryAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query, CancellationToken cancellationToken)
        {
            var text = query?.Parameters?[0]?.Value as string ?? string.Empty;
            var _searchClient = new CustomSearchClient(new ApiKeyServiceClientCredentials(_configuration["SearchSubscriptionKey"]));
            var _customConfigId = _configuration["SearchConfigurationId"];
            var results = _searchClient.CustomInstance.SearchAsync(_customConfigId, text).Result;

            var attachments = results?.WebPages?.Value?.Select(result =>
            {
                var imageUrl = HelperMethods.CheckImageUrl(result.Snippet);
                var extensionSearchResult = new ExtensionSearchResult() { Title = result.Name, ImageUrl = imageUrl, Snippet = result.Snippet, Link = result.Url };
                var previewCard = new ThumbnailCard { Title = result.Name, Tap = new CardAction { Type = "invoke", Value = extensionSearchResult } };
                previewCard.Images = new List<CardImage>() { new CardImage(imageUrl, "Icon") };

                var attachment = new MessagingExtensionAttachment
                {
                    ContentType = HeroCard.ContentType,
                    Content = new HeroCard { Title = result.Name },
                    Preview = previewCard.ToAttachment()
                };
                return attachment;
            }).ToList();

            // The list of MessagingExtensionAttachments must we wrapped in a MessagingExtensionResult wrapped in a MessagingExtensionResponse.
            return new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = attachments
                }
            };
        }

        protected override Task<MessagingExtensionResponse> OnTeamsMessagingExtensionSelectItemAsync(ITurnContext<IInvokeActivity> turnContext, JObject query, CancellationToken cancellationToken)
        {

            var article = query.ToObject<ExtensionSearchResult>();
            var card = new ThumbnailCard
            {
                Title = $"{article.Title}",
                Subtitle = article.Snippet,
                Buttons = new List<CardAction>
                    {
                        new CardAction { Type = ActionTypes.OpenUrl, Title = "Open in browser", Value = article.Link}
                    },
            };

            card.Images = new List<CardImage>() { new CardImage(article.ImageUrl, "Icon") };
            var attachment = new MessagingExtensionAttachment
            {
                ContentType = ThumbnailCard.ContentType,
                Content = card,
            };

            return Task.FromResult(new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "result",
                    AttachmentLayout = "list",
                    Attachments = new List<MessagingExtensionAttachment> { attachment }
                }
            });
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsMessagingExtensionConfigurationQuerySettingUrlAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionQuery query,
            CancellationToken cancellationToken)
        {
            // The user has requested the Messaging Extension Configuration page.  
            var escapedSettings = string.Empty;
            var userStateAccessors = UserState.CreateProperty<ExtensionSetting>(nameof(ExtensionSetting));
            var setting = await userStateAccessors.GetAsync(turnContext, () => new ExtensionSetting(), cancellationToken);

            if (!string.IsNullOrEmpty(setting.Source))
            {
                escapedSettings = Uri.EscapeDataString(setting.Source);
            }

            return new MessagingExtensionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "config",
                    SuggestedActions = new MessagingExtensionSuggestedAction
                    {
                        Actions = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type = ActionTypes.OpenUrl,
                                Value = $"https://proteamsdev.ngrok.io/extension/index?settings={escapedSettings}",
                            },
                        },
                    },
                },
            };
        }

        protected override async Task OnTeamsMessagingExtensionConfigurationSettingAsync(ITurnContext<IInvokeActivity> turnContext, JObject settings, CancellationToken cancellationToken)
        {
            // When the user submits the settings page, this event is fired.
            var state = settings["state"];
            if (state != null)
            {
                var setting = new ExtensionSetting();
                setting.Source = state.ToString();
                var userStateAccessors = UserState.CreateProperty<ExtensionSetting>(nameof(ExtensionSetting));
                await userStateAccessors.SetAsync(turnContext, setting, cancellationToken);
            }
        }

        private async Task<bool> InterruptAsync(ITurnContext<IMessageActivity> innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Activity.Type == ActivityTypes.Message)
            {
                innerDc.Activity.RemoveRecipientMention();
                var text = innerDc.Activity.Text.ToLowerInvariant().Trim();
                switch (text)
                {
                    case "help":
                    case "?":
                        var welcomeCard = CreateAdaptiveCardAttachment();
                        var response = MessageFactory.Attachment(welcomeCard);
                        await innerDc.SendActivityAsync(response, cancellationToken);
                        return true;
                    case "cancel":
                    case "quit":
                        var cancelMessage = MessageFactory.Text(CancelMsgText, CancelMsgText, InputHints.IgnoringInput);
                        await innerDc.SendActivityAsync(cancelMessage, cancellationToken);
                        return true;
                }
            }
            return false;
        }

        private Attachment CreateAdaptiveCardAttachment()
        {
            var cardResourcePath = "CompanyApp.Cards.help.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.teams.card.list",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }
    }
}
