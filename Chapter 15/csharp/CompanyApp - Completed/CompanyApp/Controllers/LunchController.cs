using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CompanyApp.Models;
using CompanyApp.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Attachment = Microsoft.Bot.Schema.Attachment;

namespace CompanyApp.Controllers
{
    public class LunchController : Controller
    {
        private readonly TeamObjectRepository _teamsObjectRepository;
        private readonly UserObjectRepository _userObjectRepository;
        private readonly IConfiguration _configuration;

        public LunchController(TeamObjectRepository teamsObjectRepository, UserObjectRepository userObjectRepository, IConfiguration configuration)
        {
            _teamsObjectRepository = teamsObjectRepository;
            _userObjectRepository = userObjectRepository;
            _configuration = configuration;
        }
        public readonly List<Lunch> LunchOptions = new List<Lunch>()
        {
            new Lunch(1,"BLT"),
            new Lunch(2,"Cheese"),
            new Lunch(3,"Ham")
        };

        public ActionResult Index()
        {
            string context = HttpContext.Request.Query["context"].ToString();
            ViewBag.UserName = HttpContext.Request.Query["name"].ToString().Split("@")[0];
            HttpContext.Session.SetString("context", context);
            if (context == "teams")
            {
                ViewBag.Layout = "_LayoutForLunch";
            }
            else
            {
                ViewBag.Layout = "_Layout";
            }

            return View(LunchOptions);
        }

        public ActionResult Order(int id, string name)
        {
            var context = HttpContext.Session.GetString("context");
            if (context == "teams")
            {
                ViewBag.Layout = "_LayoutForLunch";
            }
            else
            {
                ViewBag.Layout = "_Layout";
            }

            ViewBag.Ordermessage = $"Thanks for ordering a {LunchOptions[id].Name} sandwich.";
            return View();
        }

        public ActionResult GiveFeedback()
        {
            return View();

        }

        public ActionResult Location()
        {
            return View();
        }

        public ActionResult Auth()
        {
            return View();
        }
        public ActionResult AuthSilentEnd()
        {
            return View();
        }

        public ActionResult Admin()
        {
            return View();
        }

        public async Task<ActionResult> NotifyLunch()
        {
            var user = _userObjectRepository.GetAllAsync(Constants.UserDataPartition, 1).Result.ToList()[0];


            ConnectorClient _client = new ConnectorClient(new Uri(user.ServiceUrl),
                new MicrosoftAppCredentials(
                    _configuration["MicrosoftAppId"],
                    _configuration["MicrosoftAppPassword"],
                    new HttpClient()));

            ThumbnailCard c = new ThumbnailCard()
            {
                Title = "Lunch",
                Subtitle = "Lunch has been served",
                Text = "Your lunch is ready for you",
                Images = new List<CardImage>(),
                Buttons = new List<CardAction>(),
            };
            c.Images.Add(new CardImage { Url = "https://cdn3.iconfinder.com/data/icons/hotel-service-gray-set-1/100/Untitled-1-09-512.png" });
            Attachment card = c.ToAttachment();
            var message = MessageFactory.Attachment(card);


            var parameters = new ConversationParameters
            {
                Members = new[] { new ChannelAccount(user.UserId) },
                ChannelData = new TeamsChannelData
                {
                    Tenant = new TenantInfo(_configuration["TenantID"]),
                },
            };
            AppCredentials.TrustServiceUrl(user.ServiceUrl, DateTime.MaxValue);

            var conversationResource = await _client.Conversations.CreateConversationAsync(parameters);
            await _client.Conversations.SendToConversationAsync(conversationResource.Id, (Activity)message);

            return View("Admin");
        }

        public async Task<ActionResult> NotifyNews()
        {
            var team = _teamsObjectRepository.GetAllAsync(Constants.TeamDataPartition, 1).Result.ToList()[0];
            ConnectorClient _client = new ConnectorClient(new Uri(team.ServiceUrl),
                new MicrosoftAppCredentials(
                    _configuration["MicrosoftAppId"],
                    _configuration["MicrosoftAppPassword"],
                    new HttpClient()));


            ThumbnailCard c = new ThumbnailCard()
            {
                Title = "News",
                Subtitle = "Belgian Office is closing early",
                Text = "Since the Belgian office is closed early today please be aware that all requests must be submitted before 2 PM",
                Images = new List<CardImage>(),
                Buttons = new List<CardAction>(),
            };
            c.Images.Add(new CardImage { Url = "https://w7.pngwing.com/pngs/781/485/png-transparent-computer-icons-icon-design-open-shop-closed-store-text-logo-business.png" });
            Attachment card = c.ToAttachment();
            var message = MessageFactory.Attachment(card);

            var conversationParameters = new ConversationParameters
            {
                Bot = new ChannelAccount(team.BotId, team.BotName),
                IsGroup = true,
                ChannelData = new TeamsChannelData
                {
                    Channel = new Microsoft.Bot.Schema.Teams.ChannelInfo(team.Id),
                },
                Activity = (Activity)message,
                TenantId = _configuration["TenantID"]
            };

            AppCredentials.TrustServiceUrl(team.ServiceUrl, DateTime.MaxValue);

            var response = await _client.Conversations.CreateConversationAsync(conversationParameters);

            return View("Admin");
        }
    }
}