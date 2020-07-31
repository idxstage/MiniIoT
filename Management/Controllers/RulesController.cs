using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Utils;
using Management.Models;
using Alerting.Model;
using FileM.Models;

namespace Management.Controllers
{
    public class RulesController : Controller
    {
        private static IWebHostEnvironment _hostingEnvironment;
        private readonly Config _config;
        private readonly IMongoClient _client;
        private static IMongoDatabase _database;
        private readonly IMongoCollection<Models.Rule> _rulesCollection;
        public IActionResult Index()
        {
            return View();
        }

        public RulesController(IWebHostEnvironment hostEnvironment)
        {
            _hostingEnvironment = hostEnvironment;
            _config = Utils.Utils.ReadConfiguration();

            //inizializzo connessione con MongoDB
            _client = new MongoClient(_config.MongoDB.ConnectionString);
            _database = _client.GetDatabase("MiniIoT");
            _rulesCollection = _database.GetCollection<Models.Rule>("Rules");
        }


        public async Task<IActionResult> Modal()
        {
            return PartialView("_Modal");
        }


        public async Task<JsonResult> LoadRules(DataTableAjaxPostModel model)
        {
            
            var builder = Builders<Models.Rule>.Filter;
            String searchValue = model.search.value;
            FilterDefinition<Models.Rule> filter;

            if (!String.IsNullOrEmpty(searchValue))            
                filter = builder.Where(x => x.Machine.Contains(searchValue))
                                                    | builder.Where(x => x.Name.Contains(searchValue));            
            else            
                filter = builder.Empty;

            long totalCount = await _rulesCollection.CountDocumentsAsync(filter);

            var rules = await _rulesCollection.Find<Models.Rule>(filter).Skip(model.start).Limit(model.length).ToListAsync();

            long limitedCount = rules.Count;
            
            return Json(new
            {                
                draw = model.draw,
                recordsTotal = totalCount,
                recordsFiltered = limitedCount,
                data = rules             
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> InsertRule(Models.Rule rule)
        {
            //inserimento su database
            await _rulesCollection.InsertOneAsync(rule);
            return Json(new { result = true });
        }

    }
}
