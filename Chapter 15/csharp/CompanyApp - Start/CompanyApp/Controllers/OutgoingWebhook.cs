using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CompanyApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace CompanyApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OutgoingWebhook : ControllerBase
    {
        [HttpPost]
        public Activity AnswerBack()
        {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var userResponse = reader.ReadToEndAsync().Result;
                var teamsIncomingInformation = Newtonsoft.Json.JsonConvert.DeserializeObject<Activity>(userResponse);

                var newReaction = $"You send" + userResponse;
                var replyActivity = MessageFactory.Text(newReaction);

               var header=  Request.Headers["Authorization"];

               var authResponse = AuthProvider.Validate(
                   authenticationHeaderValue: header,
                   messageContent: userResponse,
                   claimedSenderId: teamsIncomingInformation.From.Name);

               if (authResponse.AuthSuccessful == true)
               {
                  // authentication ok, if not we could throw an exception.  
               }
                

                return replyActivity;
            }
        }
    }
}
