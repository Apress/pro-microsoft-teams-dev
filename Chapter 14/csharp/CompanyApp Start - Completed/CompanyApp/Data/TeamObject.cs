using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompanyApp.Models;
using Microsoft.Azure.Cosmos.Table;

namespace CompanyApp.Data
{
    public class TeamObject : TableEntity
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ServiceUrl { get; set; }
        public Location Location { get; set; }
        public string BotId { get; set; }
        public string BotName { get; set; }
    }
}
