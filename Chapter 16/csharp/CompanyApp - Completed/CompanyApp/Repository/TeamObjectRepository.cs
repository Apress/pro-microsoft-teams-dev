using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompanyApp.Data;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;

namespace CompanyApp.Repository
{
    public class TeamObjectRepository : TableBaseRepository<MyTeamObject>
    {
        public TeamObjectRepository(IConfiguration configuration)
            : base(configuration,
                  Constants.TeamDataTableName,
                  Constants.TeamDataPartition)
        {
        }
    }
}
