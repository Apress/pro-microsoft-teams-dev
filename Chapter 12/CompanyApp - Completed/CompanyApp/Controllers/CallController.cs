using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompanyApp.Bots;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Communications.Common.Telemetry;

namespace CompanyApp.Controllers
{
    [Route("api/call")]
    [ApiController]
    public class CallController : Controller
    {
        private readonly IGraphLogger graphLogger;
        private readonly CallBot bot;

        public CallController(CallBot bot)
        {
            this.bot = bot;
            this.graphLogger = bot.GraphLogger.CreateShim(nameof(CallController));
        }

        /// <summary>
        /// Handle call back for bot calls user case.
        /// </summary>
        /// <returns>returns when task is done.</returns>
        [HttpPost]
        public async Task OnIncomingBotCallUserRequestAsync()
        {
            await this.bot.ProcessNotificationAsync(this.Request, this.Response).ConfigureAwait(false);
        }
    }
}
