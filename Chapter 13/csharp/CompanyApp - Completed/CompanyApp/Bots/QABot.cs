using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CompanyApp.Data;
using CompanyApp.Repository;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CompanyApp.Bots
{
    public class QABot<T> : TeamsActivityHandler
        where T : Dialog
    {
        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;
        protected readonly BotState UserState;
        protected readonly ILogger Logger;
        private readonly TeamObjectRepository _teamrepository;
        private readonly UserObjectRepository _userrepository;
        private static readonly string CancelMsgText = "Cancelling...";


        public QABot(ConversationState conversationState, UserState userState, IConfiguration configuration, T dialog, ILogger<QABot<T>> logger, TeamObjectRepository teamRepository, UserObjectRepository userRepository)
        {
            ConversationState = conversationState;
            UserState = userState;
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
                        PartitionKey =  Constants.UserDataPartition,
                        RowKey = turnContext.Activity?.From?.AadObjectId,
                        UserId = turnContext.Activity?.From?.Id,
                        Name = turnContext.Activity?.From?.Name,
                        ConversationId =  turnContext.Activity?.Conversation?.Id,
                        ServiceUrl =  turnContext.Activity?.ServiceUrl
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
