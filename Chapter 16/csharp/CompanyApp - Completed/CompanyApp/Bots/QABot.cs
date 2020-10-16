using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using CompanyApp.Data;
using CompanyApp.Dialogs;
using CompanyApp.Models;
using CompanyApp.Repository;
using Microsoft.Azure.CognitiveServices.Search.CustomSearch;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DelegateAuthenticationProvider = Microsoft.Graph.DelegateAuthenticationProvider;
using GraphServiceClient = Microsoft.Graph.GraphServiceClient;

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
                    var team = new MyTeamObject()
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
            var team = new MyTeamObject()
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

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionSubmitActionAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            switch (action.CommandId)
            {
                case "orderLunchOnSaturday":
                    // either we return a hero card
                    //    return CreateCardResponse(turnContext, action);

                    //  Or we return a task module
                    var response = new MessagingExtensionActionResponse()
                    {
                        Task = new TaskModuleContinueResponse()
                        {
                            Value = new TaskModuleTaskInfo()
                            {
                                Height = "small",
                                Width = "small",
                                Title = "Thanks for your feedback",
                                Url = "https://proteamsdev.ngrok.io/extension/thanks",
                            },
                        },
                    };
                    return response;


                default:
                    throw new NotImplementedException($"Invalid CommandId: {action.CommandId}");
            }
        }

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionBotMessagePreviewEditAsync(
            ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            var response = new MessagingExtensionActionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    Type = "botMessagePreview",
                    ActivityPreview = MessageFactory.Attachment(new Attachment
                    {
                        Content = new AdaptiveCard("1.0")
                        {
                            Body = new List<AdaptiveElement>()
                            {
                                new AdaptiveTextBlock() { Text = "This is your adaptive card", Size = AdaptiveTextSize.Large },
                                new AdaptiveTextBlock() { Text = "Press open URL to open a URL", Size = AdaptiveTextSize.Small}
                            },
                            Height = AdaptiveHeight.Auto,
                            Actions = new List<AdaptiveAction>()
                            {
                                new AdaptiveOpenUrlAction()
                                {
                                    Title = "Open",
                                    Url = new Uri("https://proteamsdev.ngrok.io/lunch")
                                },
                            }
                        },
                        ContentType = AdaptiveCard.ContentType
                    }) as Activity
                }
            };
            return response;
        }

        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionBotMessagePreviewSendAsync(
            ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            var activityPreview = action.BotActivityPreview[0];
            var attachmentContent = activityPreview.Attachments[0].Content;
            var previewedCard = JsonConvert.DeserializeObject<AdaptiveCard>(attachmentContent.ToString(),
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            previewedCard.Version = "1.0";

            var responseActivity = Activity.CreateMessageActivity();
            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = previewedCard
            };
            responseActivity.Attachments.Add(attachment);

            // Attribute the message to the user on whose behalf the bot is posting
            responseActivity.ChannelData = new
            {
                OnBehalfOf = new[]
                {
                    new
                    {
                        ItemId = 0,
                        MentionType = "person",
                        Mri = turnContext.Activity.From.Id,
                        DisplayName = turnContext.Activity.From.Name
                    }
                }
            };

            await turnContext.SendActivityAsync(responseActivity);

            return new MessagingExtensionActionResponse();
        }



        protected override async Task<MessagingExtensionActionResponse> OnTeamsMessagingExtensionFetchTaskAsync(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action, CancellationToken cancellationToken)
        {
            var magicCode = string.Empty;
            var state = ((JObject)turnContext.Activity.Value).Value<string>("state");
            if (!string.IsNullOrEmpty(state))
            {
                if (int.TryParse(state, out var parsed))
                {
                    magicCode = parsed.ToString();
                }
            }
            var tokenResponse = await ((IUserTokenProvider)turnContext.Adapter).GetUserTokenAsync(turnContext, "GetLocation", magicCode, cancellationToken: cancellationToken);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.Token))
            {
                var signInLink = await (turnContext.Adapter as IUserTokenProvider).GetOauthSignInLinkAsync(turnContext, "GetLocation", cancellationToken);
                return new MessagingExtensionActionResponse
                {
                    ComposeExtension = new MessagingExtensionResult
                    {
                        Type = "auth",
                        SuggestedActions = new MessagingExtensionSuggestedAction
                        {
                            Actions = new List<CardAction>
                            {
                                new CardAction
                                {
                                    Type = ActionTypes.OpenUrl,
                                    Value = signInLink,
                                    Title = "Sign in Please",
                                },
                            },
                        },
                    },
                };
            }
            var accessToken = tokenResponse.Token;
            if (accessToken != null || !string.IsNullOrEmpty(accessToken))
            {
                //We return our lunch ordering page with additional information in the query string

                //var client = GetGraphServiceClient(accessToken);
                //var user = await client.Me
                //    .Request()
                //    .GetAsync();

                //var response = new MessagingExtensionActionResponse()
                //{
                //    Task = new TaskModuleContinueResponse()
                //    {
                //        Value = new TaskModuleTaskInfo()
                //        {
                //            Height = "medium",
                //            Width = "medium",
                //            Title = "Order lunch",
                //            Url = $"https://proteamsdev.ngrok.io/lunch?name={user.OfficeLocation}&context=teams",
                //        },
                //    },
                //};
                //return response;

                // We can also return an adaptive card - uncomment the following section and comment the previous response

                //var response = new MessagingExtensionActionResponse
                //{
                //    Task = new TaskModuleContinueResponse
                //    {
                //        Value = new TaskModuleTaskInfo
                //        {
                //            Card = new Attachment
                //            {
                //                Content = new AdaptiveCard("1.0")
                //                {
                //                    Body = new List<AdaptiveElement>()
                //                    {
                //                        new AdaptiveTextBlock()
                //                            {Text = "This is your adaptive card", Size = AdaptiveTextSize.Large},
                //                        new AdaptiveTextBlock()
                //                            {Text = "Press open URL to open a URL", Size = AdaptiveTextSize.Small}
                //                    },
                //                    Height = AdaptiveHeight.Auto,
                //                    Actions = new List<AdaptiveAction>()
                //                    {
                //                        new AdaptiveOpenUrlAction()
                //                        {
                //                            Title = "Open",
                //                            Url = new Uri("https://proteamsdev.ngrok.io/lunch")
                //                        },
                //                    }
                //                },
                //                ContentType = AdaptiveCard.ContentType
                //            },
                //            Title = "Adaptive Card",
                //            Height = 300,
                //            Width = 600,
                //        }
                //    }
                //};
                //return response;

                // We can also return an adaptive card as a bot preview - uncomment the following section and comment the previous response
                var response = new MessagingExtensionActionResponse
                {
                    ComposeExtension = new MessagingExtensionResult
                    {
                        Type = "botMessagePreview",
                        ActivityPreview = MessageFactory.Attachment(new Attachment
                        {
                            Content = new AdaptiveCard("1.0")
                            {
                                Body = new List<AdaptiveElement>()
                                {
                                    new AdaptiveTextBlock() { Text = "This is your adaptive card", Size = AdaptiveTextSize.Large },
                                    new AdaptiveTextBlock() { Text = "Press open URL to open a URL", Size = AdaptiveTextSize.Small}
                                },
                                Height = AdaptiveHeight.Auto,
                                Actions = new List<AdaptiveAction>()
                                {
                                    new AdaptiveOpenUrlAction()
                                    {
                                        Title = "Open",
                                        Url = new Uri("https://proteamsdev.ngrok.io/lunch")
                                    },
                                }
                            },
                            ContentType = AdaptiveCard.ContentType
                        }) as Activity
                    }
                };
                return response;
            }
            return null;
        }

        protected override async Task<MessagingExtensionResponse> OnTeamsAppBasedLinkQueryAsync(ITurnContext<IInvokeActivity> turnContext, AppBasedLinkQuery query, CancellationToken cancellationToken)
        {
            var card = new HeroCard
            {
                Title = "You are referencing our domain",
                Text = query.Url,
                Images = new List<CardImage> { new CardImage("https://upload.wikimedia.org/wikipedia/commons/thumb/c/c9/Microsoft_Office_Teams_%282018%E2%80%93present%29.svg/1200px-Microsoft_Office_Teams_%282018%E2%80%93present%29.svg.png") },
            };

            var attachments = new MessagingExtensionAttachment(HeroCard.ContentType, null, card);
            var result = new MessagingExtensionResult(AttachmentLayoutTypes.List, "result", new[] { attachments }, null, "Our domain unfurling");

            return new MessagingExtensionResponse(result);
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

        private MessagingExtensionActionResponse CreateCardResponse(ITurnContext<IInvokeActivity> turnContext, MessagingExtensionAction action)
        {
            var formInformation = JsonConvert.DeserializeObject<StaticSaturdayLunchResponse>(action.Data.ToString());
            var card = new HeroCard
            {
                Title = "Success",
                Subtitle = "You lunch has been ordered",
                Text = $"Dear {formInformation.Name}. Your lunch {formInformation.Choice} is ordered and will be ready for your on {formInformation.Date}",
            };

            var attachments = new List<MessagingExtensionAttachment>();
            attachments.Add(new MessagingExtensionAttachment
            {
                Content = card,
                ContentType = HeroCard.ContentType,
                Preview = card.ToAttachment(),
            });

            return new MessagingExtensionActionResponse
            {
                ComposeExtension = new MessagingExtensionResult
                {
                    AttachmentLayout = "list",
                    Type = "result",
                    Attachments = attachments,
                },
            };
        }

        private GraphServiceClient GetGraphServiceClient(string token)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    requestMessage =>
                    {
                // Append the access token to the request.
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                        return Task.CompletedTask;
                    }));
            return graphClient;
        }
    }
}
