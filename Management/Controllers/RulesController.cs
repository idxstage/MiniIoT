using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Utils;

namespace Management.Controllers
{
    public class RulesController : Controller
    {
        private static IWebHostEnvironment _hostingEnvironment;
        private readonly Config _config;

        public IActionResult Index()
        {
            return View();
        }

        public RulesController(IWebHostEnvironment hostEnvironment)
        {
            _hostingEnvironment = hostEnvironment;
            var path = Path.Combine(_hostingEnvironment.WebRootPath, "config.json");
        }



    }
}
