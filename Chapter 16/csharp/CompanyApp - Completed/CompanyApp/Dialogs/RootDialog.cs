using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.CognitiveServices.Search.CustomSearch;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Teams;

namespace CompanyApp.Dialogs
{
    public class RootDialog : ComponentDialog
    {
        protected readonly ILogger Logger;
        private readonly CustomSearchClient _searchClient;
        private readonly string _customConfigId;
        private readonly QnAMaker _qnaMaker;


        public RootDialog(IConfiguration configuration, ILogger<RootDialog> logger, IHttpClientFactory httpClientFactory)
        {
            Logger = logger;
            var httpClient = httpClientFactory.CreateClient();
            _searchClient = new CustomSearchClient(new ApiKeyServiceClientCredentials(configuration["SearchSubscriptionKey"]));
            _customConfigId = configuration["SearchConfigurationId"];

            QnAMakerOptions qnaMakerOptions = new QnAMakerOptions
            {
                ScoreThreshold = float.Parse(configuration["QnAScoreThreshold"], CultureInfo.InvariantCulture.NumberFormat)
            };

            _qnaMaker = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = configuration["QnAKnowledgebaseId"],
                EndpointKey = configuration["QnAEndpointKey"],
                Host = configuration["QnAEndpointHostName"]
            }, qnaMakerOptions, httpClient);
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                InitialStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var teamInfo = stepContext.Context.Activity.TeamsGetTeamInfo();

            if (teamInfo?.Id != null && stepContext.Options != null) // inside team
            {
                Logger.LogInformation("We are inside a Team. No need to be nice to the user and just respond with the answer. Skipping this step.");
                var message = stepContext.Context.Activity.RemoveRecipientMention();
                return await stepContext.NextAsync(message, cancellationToken);
            }
            else //inside private or group chat
            {
                // Use the text provided in FinalStepAsync or the default if it is the first time.
                var messageText = stepContext.Options?.ToString() ?? "What can I help you with today?\nSay something like \"How do I set my Outlook signature\"";
                var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {



            var searchTerm = (string)stepContext.Result;
            //Check for chit chat
            var metadata = new Metadata();
            var qnaOptions = new QnAMakerOptions();
            metadata.Name = "editorial";
            metadata.Value = "chitchat";
            qnaOptions.StrictFilters = new[] { metadata };
            qnaOptions.Top = 1;
            qnaOptions.ScoreThreshold = 0.9F;
            //Search our QnA service
            var isChitChat = await _qnaMaker.GetAnswersAsync(stepContext.Context, qnaOptions);

            if (isChitChat != null && isChitChat.Length > 0)
            {

                // Restart the main dialog with a different message the second time around
                return await stepContext.ReplaceDialogAsync(InitialDialogId, isChitChat[0].Answer, cancellationToken);
            }



            //Search Microsoft
            var results = _searchClient.CustomInstance.SearchAsync(_customConfigId, searchTerm).Result;

            if (results?.WebPages?.Value?.Count > 3)
            {
                var msAnswer = $"These answers where delived by Microsoft";
                await stepContext.Context.SendActivityAsync(msAnswer);
                var getFeedback = stepContext.Context.Activity.CreateReply();
                for (int i = 0; i < 3; i++)
                {
                    var result = results.WebPages.Value[i];
                    var imageUrl = HelperMethods.CheckImageUrl(result.Snippet);
                    var heroCard = new ThumbnailCard
                    {
                        Title = result.Name,
                        Subtitle = result.DisplayUrl,
                        Text = result.Snippet,
                        Images = new List<CardImage> { new CardImage(imageUrl) },
                        Buttons = new List<CardAction> { new CardAction(ActionTypes.OpenUrl, "Open this solution", value: result.Url) },
                    };
                    // Add the attachment to our reply.
                    getFeedback.Attachments.Add(heroCard.ToAttachment());
                }
                getFeedback.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                await stepContext.Context.SendActivityAsync(getFeedback, cancellationToken);
            }
            else
            {
                var msAnswerNotFound = $"Sorry I did not find anything online.";
                await stepContext.Context.SendActivityAsync(msAnswerNotFound);
            }


            //Search our QnA service
            var response = await _qnaMaker.GetAnswersAsync(stepContext.Context);

            if (response != null && response.Length > 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);
            }
            else
            {
                var msAnswerNotFound = $"Sorry I did not find anything in the company knowledge base.";
                await stepContext.Context.SendActivityAsync(msAnswerNotFound);
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var promptMessage = "What else can I help you with?";

            // Restart the main dialog with a different message the second time around
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
