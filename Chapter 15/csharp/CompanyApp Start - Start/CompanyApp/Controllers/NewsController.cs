using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CompanyApp.Data;
using CompanyApp.Models;
using CompanyApp.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CompanyApp.Controllers
{
    public class NewsController : Controller
    {
        private readonly TeamObjectRepository _teamsRepository;

        public NewsController(TeamObjectRepository teamsRepository)
        {
            _teamsRepository = teamsRepository;
        }


        public readonly List<NewsItem> NewsItems = new List<NewsItem>()
        {
            new NewsItem(1,Location.America, "Seattle office voted best in sales","This month our Seattle Office was voted best in sales at the Seattle Small and Medium Business Owners Convention. Also known as SSMBOC"),
            new NewsItem(2,Location.Europe, "Belgium office closed because of renovation","Friday was the last day at the office for most of our Belgian employees, they will be working from home for then next month while the much needed renovations are starting."),
            new NewsItem(3,Location.Asia, "Japan office move is getting started","Since the purchase of the new highrise for our Japas office, our co-workers there have started with preparing everything for the big move. They are happy and sad at the same time while they prepare to leave our original starting location"),
        };


        public IActionResult Index([FromQuery] string location)
        {
            Enum.TryParse(location, out Location submittedLocation);
            return View(NewsItems.Where(x => x.Location == submittedLocation));
        }
        public IActionResult Configure()
        {
            LocationModel m = new LocationModel();
            return View(m);
        }

        [HttpPost("SaveConfiguration")]
        public async Task<HttpResponseMessage> SaveConfiguration()
        {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var jsonString = reader.ReadToEndAsync().Result;
                var config = Newtonsoft.Json.JsonConvert.DeserializeObject<NewsConfigurationArray>(jsonString);


                Enum.TryParse(config.NewsConfiguration[0].location, out Location submittedLocation);

                var team = new TeamObject()
                {
                    PartitionKey = Constants.TeamDataPartition,
                    RowKey = config.NewsConfiguration[0].channelId,
                    Id = config.NewsConfiguration[0].channelId,
                    Location = submittedLocation
                };
                await _teamsRepository.CreateOrUpdateAsync(team);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        public IActionResult Remove()
        {
            return View();
        }

        [HttpPost]
        public IActionResult BirthdayConfirmation()
        {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var userResponse = reader.ReadToEndAsync().Result;
                var responseMessage = "{\"text\": \"Sorry you can not make it\" }";
                Request.Query.TryGetValue("answer", out var answer);

                if (answer != "no")
                {
                    responseMessage = "{\"text\": \"Thanks for registering and giving the following comment: " +
                                      userResponse + "\"}";
                }

                Response.Headers.Add("CARD-UPDATE-IN-BODY", "true");
                return StatusCode(200, responseMessage);
            }
        }
    }
}