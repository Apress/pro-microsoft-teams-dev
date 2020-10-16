﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CompanyApp.Authentication;
using CompanyApp.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.Graph;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Client.Transport;
using Microsoft.Graph.Communications.Common;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Common.Transport;
using Microsoft.Graph.Communications.Core.Notifications;
using Microsoft.Graph.Communications.Core.Serialization;
using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json;
using File = Microsoft.Graph.File;

namespace CompanyApp.Bots
{
    public class CallBot
    {
        private readonly IConfiguration _configuration;

        public CallBot(IConfiguration configuration, IGraphLogger graphLogger)
        {
            _configuration = configuration;

            this.GraphLogger = graphLogger;
            var name = this.GetType().Assembly.GetName().Name;
            this.AuthenticationProvider = new AuthenticationProvider(name, configuration["MicrosoftAppId"], configuration["MicrosoftAppPassword"], graphLogger);
            this.Serializer = new CommsSerializer();

            var authenticationWrapper = new AuthenticationWrapper(this.AuthenticationProvider);
            this.NotificationProcessor = new NotificationProcessor(authenticationWrapper, this.Serializer);
            this.NotificationProcessor.OnNotificationReceived += this.NotificationProcessor_OnNotificationReceived;
            this.RequestBuilder = new GraphServiceClient("https://graph.microsoft.com/v1.0", authenticationWrapper);

            // Add the default headers used by the graph client.
            // This will include SdkVersion.
            var defaultProperties = new List<IGraphProperty<IEnumerable<string>>>();
            using (HttpClient tempClient = GraphClientFactory.Create(authenticationWrapper))
            {
                defaultProperties.AddRange(tempClient.DefaultRequestHeaders.Select(header => GraphProperty.RequestProperty(header.Key, header.Value)));
            }

            // graph client
            var productInfo = new ProductInfoHeaderValue(
                typeof(CallBot).Assembly.GetName().Name,
                typeof(CallBot).Assembly.GetName().Version.ToString());
            this.GraphApiClient = new GraphAuthClient(
                this.GraphLogger,
                this.Serializer.JsonSerializerSettings,
                new HttpClient(),
                this.AuthenticationProvider,
                productInfo,
                defaultProperties);
        }

        /// <summary>
        /// Gets graph logger.
        /// </summary>
        public IGraphLogger GraphLogger { get; }

        /// <summary>
        /// Gets the authentication provider.
        /// </summary>
        public IRequestAuthenticationProvider AuthenticationProvider { get; }

        /// <summary>
        /// Gets the notification processor.
        /// </summary>
        public INotificationProcessor NotificationProcessor { get; }

        /// <summary>
        /// Gets the URI builder.
        /// </summary>
        public GraphServiceClient RequestBuilder { get; }

        /// <summary>
        /// Gets the serializer.
        /// </summary>
        public CommsSerializer Serializer { get; }

        /// <summary>
        /// Gets the stateless graph client.
        /// </summary>
        public IGraphClient GraphApiClient { get; }

        /// <summary>
        /// Processes the notification asynchronously.
        /// Here we make sure we log the http request and catch/log any errors.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task ProcessNotificationAsync(
            HttpRequest request,
            HttpResponse response)
        {
            // TODO: Parse out the scenario id and request id headers.
            var headers = request.Headers.Select(
                pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

            // Don't log content since we can't PII scrub here (we don't know the type).
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            this.GraphLogger.LogHttpMessage(
                TraceLevel.Verbose,
                TransactionDirection.Incoming,
                HttpTraceType.HttpRequest,
                request.GetDisplayUrl(),
                request.Method,
                obfuscatedContent: null,
                headers: headers);

            try
            {
                var httpRequest = request.CreateRequestMessage();
                var results = await this.AuthenticationProvider.ValidateInboundRequestAsync(httpRequest).ConfigureAwait(false);
                if (results.IsValid)
                {
                    var httpResponse = await this.NotificationProcessor.ProcessNotificationAsync(httpRequest).ConfigureAwait(false);
                    await httpResponse.CreateHttpResponseAsync(response).ConfigureAwait(false);
                }
                else
                {
                    var httpResponse = httpRequest.CreateResponse(HttpStatusCode.Forbidden);
                    await httpResponse.CreateHttpResponseAsync(response).ConfigureAwait(false);
                }

                headers = response.Headers.Select(
                    pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

                this.GraphLogger.LogHttpMessage(
                    TraceLevel.Verbose,
                    TransactionDirection.Incoming,
                    HttpTraceType.HttpResponse,
                    request.GetDisplayUrl(),
                    request.Method,
                    obfuscatedContent: null,
                    headers: headers,
                    responseCode: response.StatusCode,
                    responseTime: stopwatch.ElapsedMilliseconds);
            }
            catch (ServiceException e)
            {
                string obfuscatedContent = null;
                if ((int)e.StatusCode >= 300)
                {
                    response.StatusCode = (int)e.StatusCode;
                    await response.WriteAsync(e.ToString()).ConfigureAwait(false);
                    obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(e, Formatting.Indented);
                }
                else if ((int)e.StatusCode >= 200)
                {
                    response.StatusCode = (int)e.StatusCode;
                }
                else
                {
                    response.StatusCode = (int)e.StatusCode;
                    await response.WriteAsync(e.ToString()).ConfigureAwait(false);
                    obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(e, Formatting.Indented);
                }

                headers = response.Headers.Select(
                    pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

                if (e.ResponseHeaders?.Any() == true)
                {
                    foreach (var pair in e.ResponseHeaders)
                    {
                        response.Headers.Add(pair.Key, new StringValues(pair.Value.ToArray()));
                    }

                    headers = headers.Concat(e.ResponseHeaders);
                }

                this.GraphLogger.LogHttpMessage(
                    TraceLevel.Error,
                    TransactionDirection.Incoming,
                    HttpTraceType.HttpResponse,
                    request.GetDisplayUrl(),
                    request.Method,
                    obfuscatedContent,
                    headers,
                    response.StatusCode,
                    responseTime: stopwatch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await response.WriteAsync(e.ToString()).ConfigureAwait(false);

                var obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(e, Formatting.Indented);
                headers = response.Headers.Select(
                    pair => new KeyValuePair<string, IEnumerable<string>>(pair.Key, pair.Value));

                this.GraphLogger.LogHttpMessage(
                    TraceLevel.Error,
                    TransactionDirection.Incoming,
                    HttpTraceType.HttpResponse,
                    request.GetDisplayUrl(),
                    request.Method,
                    obfuscatedContent,
                    headers,
                    response.StatusCode,
                    responseTime: stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Raised when the <see cref="INotificationProcessor"/> has received a notification.
        /// </summary>
        /// <param name="args">The <see cref="NotificationEventArgs"/> instance containing the event data.</param>
        private void NotificationProcessor_OnNotificationReceived(NotificationEventArgs args)
        {
#pragma warning disable 4014
            // Processing notification in the background.
            // This ensures we're not holding on to the request.
            this.NotificationProcessor_OnNotificationReceivedAsync(args).ForgetAndLogExceptionAsync(
                this.GraphLogger,
                $"Error processing notification {args.Notification.ResourceUrl} with scenario {args.ScenarioId}");
#pragma warning restore 4014
        }

        /// <summary>
        /// Raised when the <see cref="INotificationProcessor"/> has received a notification asynchronously.
        /// </summary>
        /// <param name="args">The <see cref="NotificationEventArgs"/> instance containing the event data.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task NotificationProcessor_OnNotificationReceivedAsync(NotificationEventArgs args)
        {
            this.GraphLogger.CorrelationId = args.ScenarioId;
            var headers = new[]
            {
                new KeyValuePair<string, IEnumerable<string>>(HttpConstants.HeaderNames.ScenarioId, new[] { args.ScenarioId.ToString() }),
                new KeyValuePair<string, IEnumerable<string>>(HttpConstants.HeaderNames.ClientRequestId, new[] { args.RequestId.ToString() }),
                new KeyValuePair<string, IEnumerable<string>>(HttpConstants.HeaderNames.Tenant, new[] { args.TenantId }),
            };

            // Create obfuscation content to match what we
            // would have gotten from the service, then log.
            var notifications = new CommsNotifications { Value = new[] { args.Notification } };
            var obfuscatedContent = this.GraphLogger.SerializeAndObfuscate(notifications, Formatting.Indented);
            this.GraphLogger.LogHttpMessage(
                TraceLevel.Info,
                TransactionDirection.Incoming,
                HttpTraceType.HttpRequest,
                args.CallbackUri.ToString(),
                HttpMethods.Post,
                obfuscatedContent,
                headers,
                correlationId: args.ScenarioId,
                requestId: args.RequestId);

            if (args.ResourceData is Call call)
            {
                if (args.ChangeType == ChangeType.Created && call.State == CallState.Incoming)
                {
                    await this.BotAnswerIncomingCallAsync(call.Id, args.TenantId, args.ScenarioId).ConfigureAwait(false);
                }
                else if (args.ChangeType == ChangeType.Updated && (call.State == CallState.Established || call.State == CallState.Establishing))
                {
                    this.GraphLogger.Log(TraceLevel.Info, $"In {call.State}");

                    if (call.ToneInfo == null && call.MediaState?.Audio == MediaState.Active)
                    {
                        await this.BotPlayNotificationPromptAsync(call.Id, args.TenantId, args.ScenarioId).ConfigureAwait(false);
                    }
                    else if (call.ToneInfo?.SequenceId == 1)
                    {
                        InvitationParticipantInfo transferTarget = null;						 
                        if (call.ToneInfo.Tone == Tone.Tone1)
                        {
                            transferTarget = new InvitationParticipantInfo
                            {
                                Identity = new IdentitySet
                                {
                                    User = new Identity
                                    {
                                        Id = "ObjectIds:First",
                                    },
                                },
                            };
                        }
                        else
                        {
                            transferTarget = new InvitationParticipantInfo
                            {
                               Identity = new IdentitySet
                               {
                                   User = new Identity
                                   {
                                      Id = "ObjectIds:Third",
                                   },
                               },
                            };
                        }

                        await this.BotTransferCallAsync(transferTarget, call.Id, args.TenantId, args.ScenarioId).ConfigureAwait(false);
                    }
                }
                else if (args.ChangeType == ChangeType.Deleted && call.State == CallState.Terminated)
                {
                   this.GraphLogger.Log(TraceLevel.Info, $"Call State:{call.State}");
                }
            }
            else if (args.ResourceData is PlayPromptOperation playPromptOperation)
            {
                //  checking for the call id sent in ClientContext.
                if (string.IsNullOrWhiteSpace(playPromptOperation.ClientContext))
                {
                    throw new ServiceException(new Error()
                    {
                        Message = "No call id provided in PlayPromptOperation.ClientContext.",
                    });
                }
                else if (playPromptOperation.Status == OperationStatus.Completed)
                {
                    await this.BotSubscribesToToneAsync(playPromptOperation.ClientContext, args.TenantId, args.ScenarioId).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Subscribes to Tone.
        /// </summary>
        /// <param name="callId">The call identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task BotSubscribesToToneAsync(string callId, string tenantId, Guid scenarioId)
        {
            await this.GraphApiClient.SendAsync(this.RequestBuilder.Communications.Calls[callId].SubscribeToTone(callId).Request(), RequestType.Create, tenantId, scenarioId).ConfigureAwait(false);
        }

        /// <summary>
        /// Bot answers incoming call.
        /// </summary>
        /// <param name="callId">The identifier of the call to transfer.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task BotAnswerIncomingCallAsync(string callId, string tenantId, Guid scenarioId)
        {
            var fileName = await GenerateTTSFile("Hello from Microsoft Teams.");

            var answerRequest = this.RequestBuilder.Communications.Calls[callId].Answer(
                callbackUri: new Uri(_configuration["ngrokURL"] + "api/call").ToString(),
                mediaConfig: new ServiceHostedMediaConfig
                { 
                    PreFetchMedia = new List<MediaInfo>()
                    {
                        new MediaInfo()
                        {
                            Uri = new Uri(_configuration["ngrokURL"] + fileName).ToString(),
                            ResourceId = Guid.NewGuid().ToString(),
                        },
                    },
				},
                acceptedModalities: new List<Modality> { Modality.Audio }).Request();
            await this.GraphApiClient.SendAsync(answerRequest, RequestType.Create, tenantId, scenarioId).ConfigureAwait(false);
        }

        /// <summary>
        /// Bot transfers incoming call.
        /// </summary>
        /// <param name="target">The identifier of the transfer target.</param>
        /// <param name="callId">The call identifier.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task BotTransferCallAsync(InvitationParticipantInfo target, string callId, string tenantId, Guid scenarioId)
        {
            await this.GraphApiClient.SendAsync(this.RequestBuilder.Communications.Calls[callId].Transfer(target).Request(), RequestType.Create, tenantId, scenarioId).ConfigureAwait(false);
        }

        /// <summary>
        /// Bot plays notification.
        /// </summary>
        /// <param name="callId">The call identifier.</param>
        /// <param name="tenantId">The Tenant identifier.</param>
        /// <param name="scenarioId">The scenario identifier.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task BotPlayNotificationPromptAsync(string callId, string tenantId, Guid scenarioId)
        {


            var fileName = await GenerateTTSFile("Hello from Microsoft Teams. I will start the recording now. I still have a question about skype.");
            var sb = new StringBuilder();

            var ttsMedia = new MediaInfo
            {
                Uri = new Uri(_configuration["ngrokURL"] + fileName).ToString(),
                ResourceId = Guid.NewGuid().ToString(),
            };
            var ttsMediaPrompt = new MediaPrompt() { MediaInfo = ttsMedia };

            var playPromptRequest = this.RequestBuilder.Communications.Calls[callId].PlayPrompt(
                prompts: new List<MediaPrompt> { ttsMediaPrompt },
                clientContext: callId).Request();

            await this.GraphApiClient.SendAsync<PlayPromptOperation>(playPromptRequest, RequestType.Create, tenantId, scenarioId).ConfigureAwait(false);
        }


         public async Task<string> GenerateTTSFile(string message)
        {
            var filenameGuid = Guid.NewGuid();
            var fileName = $"wwwroot/audio/{filenameGuid}.wav";
            await GetTTS(fileName, message);
            return fileName;
        }
         
        private async Task GetTTS(string fileName, string message)
        {
            var config = SpeechConfig.FromSubscription("646cf1c565854d24b691a0abce444fbc", "eastus");

            using (var fileOutput = AudioConfig.FromWavFileOutput(fileName))
            {
                using (var synthesizer = new SpeechSynthesizer(config, fileOutput))
                {
                    var result = await synthesizer.SpeakTextAsync(message);
                }
            }
        }
    }
}