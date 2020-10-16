using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompanyApp.Models
{


    public class NewsConfigurationArray
    {
        public Newsconfiguration[] NewsConfiguration { get; set; }
    }

    public class Newsconfiguration
    {
        public string location { get; set; }
        public string channelId { get; set; }
        public string groupId { get; set; }
        public string teamId { get; set; }
    }


}
