using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompanyApp.Data;
using Microsoft.Extensions.Configuration;

namespace CompanyApp.Repository
{
    public class UserObjectRepository : TableBaseRepository<UserObject>
    {
        public UserObjectRepository(IConfiguration configuration) : base(
                configuration,
                Constants.UserDataTableName,
                Constants.UserDataPartition
                )
        {
        }
    }
}
