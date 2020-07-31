using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Management.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Utils;
using System.Net.Http;
using javax.jws;
using com.sun.corba.se.impl.protocol.giopmsgheaders;
using javax.xml.crypto;

namespace Management.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static IWebHostEnvironment _hostingEnvironment;
        private static Config _config;
        static readonly HttpClient client = new HttpClient();

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostEnvironment;

            _config = Utils.Utils.ReadConfiguration();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<string> GetTiming()
        {
            return _config.Monitoring.Timing.ToString();
        }

        public async Task<string> CheckAll()
        {
            string status = "";
            foreach (Modulo m in _config.Monitoring.Modules)
                status += CheckAvailability(m).Result;

            // rimuoviamo l'ultimo ";"
            return status.Remove(status.Length - 1, 1);
        }


        public static async Task<string> CheckAvailability(Modulo modulo)
        {
            var path = Path.Combine(_hostingEnvironment.WebRootPath, "config.json");
            try
            {
                // prepariamo e inviamo la richiesta
                string uri = $"http://{modulo.Ip}:{modulo.Port}/";

                string response = await client.GetStringAsync(uri);

                // analizziamo la risposta
                if (response != null)
                    return $"{modulo.Name}:1;";
                else
                    return $"{modulo.Name}:0;";
            }
            catch (HttpRequestException ex)
            {
                return $"{modulo.Name}:0;";
            }
        }
    }
}
