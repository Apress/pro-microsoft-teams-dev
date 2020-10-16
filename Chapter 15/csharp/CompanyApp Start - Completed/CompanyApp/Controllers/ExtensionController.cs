using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CompanyApp.Controllers
{
    public class ExtensionController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
