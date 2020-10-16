using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompanyApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CompanyApp.Controllers
{
    public class LunchController : Controller
    {
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

        // GET: Lunch/Create
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

    }
}