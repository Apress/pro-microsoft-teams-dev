using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CompanyApp.Controllers
{
    public class ConnectorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Configure()
        {
            return View();
        }
        [HttpPost("Save")]
        public async Task<HttpResponseMessage> Save()
        {
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var jsonString = reader.ReadToEndAsync().Result;
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}
