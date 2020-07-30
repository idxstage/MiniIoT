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

namespace Management.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static IWebHostEnvironment _hostingEnvironment;
        private static Config _config;
        private readonly IHttpClientFactory _clientFactory;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostingEnvironment = hostEnvironment;
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

        public string CheckAll()
        {
            string status = "";
            foreach (Modulo m in _config.Monitoring.Modules)
                status += CheckAvailability(m);

            // rimuoviamo l'ultimo ";"
            return status.Remove(status.Length - 1, 1);
        }

     
        public async Task<string> CheckAvailability(Modulo modulo)
        {
            var path = Path.Combine(_hostingEnvironment.WebRootPath, "config.json");

            // creiamo il client che contatterà il modulo esterno
            var client = _clientFactory.CreateClient();

            // prepariamo la risposta
            var request = new HttpRequestMessage(HttpMethod.Get,
            $"http://{modulo.Ip}:{modulo.Port}");
            request.Headers.Add("Accept", "application/vnd.github.v3+json");
            request.Headers.Add("User-Agent", "HttpClientFactory-Sample");
            
            // inviamo la richiesta
            var response = await client.SendAsync(request);

            // analizziamo la risposta
            try
            {
                if (response.IsSuccessStatusCode)
                    return $"{modulo.Name}:1;";
                else
                    return $"{modulo.Name}:0;";        
            }
            catch (Exception e)
            {
                return $"{modulo.Name}:0;";
            }

        }
    }
}
