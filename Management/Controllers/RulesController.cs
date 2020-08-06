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
using com.sun.org.apache.xpath.@internal.axes;
using System.Net.Http;
using log4net;
using System.Reflection;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using com.sun.xml.@internal.fastinfoset.util;

namespace Management.Controllers
{
    public class RulesController : Controller
    {
        private static IWebHostEnvironment _hostingEnvironment;
        private readonly Config _config;
        private readonly IMongoClient _client;
        private static IMongoDatabase _database;
        private readonly IMongoCollection<Models.Rule> _rulesCollection;
        static readonly HttpClient client = new HttpClient();
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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


        public async Task<IActionResult> Modal(String mode, String id)
        {
            if (mode.Equals("add"))
            {
                ViewBag.mode = "add";
                return PartialView("_Modal");
            }
            else
            {
                ViewBag.mode = "edit";
                var rule = await _rulesCollection.Find(r => r.Id == id).FirstOrDefaultAsync();
                return PartialView("_Modal", rule);
            }
           
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

        
        public async Task<JsonResult> UpdateRule(Models.Rule rule)
        {

            var result = await _rulesCollection.UpdateOneAsync(r => r.Id == rule.Id, Builders<Models.Rule>.Update
                                                                                                            .Set(r => r.Machine, rule.Machine)
                                                                                                            .Set(r => r.Name, rule.Name)
                                                                                                            .Set(r => r.Period, rule.Period)
                                                                                                            .Set(r => r.Frequency, rule.Frequency)
                                                                                                            .Set(r => r.Severity, rule.Severity)
                                                                                                            .Set(r => r.Field, rule.Field)
                                                                                                            .Set(r => r.ConditionOperator, rule.ConditionOperator)
                                                                                                            .Set(r => r.Value, rule.Value)
                                                                                                            .Set(r => r.actions, rule.actions));

            if (result.IsAcknowledged && result.ModifiedCount > 0)
                return Json(new { result = true });
            else
                return Json(new { result = false });

        }

        public async Task<JsonResult> DeleteRule(String id)
        {

            var result = await _rulesCollection.DeleteOneAsync(r => r.Id == id);
            if (result.IsAcknowledged && result.DeletedCount > 0)            
                return Json(new { result = true });           
            else
                return Json(new { result = false });
        }

        public async void SendThreshold()
        {
            try
            {
                //leggo json dashboard
                var currentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var filePath = $"{currentPath}\\Dashboard.json";
                GrafanaModel model;

                if (System.IO.File.Exists(filePath))
                {
                   

                    using var r = new StreamReader(filePath);
                    var json = r.ReadToEnd();
                    model = Newtonsoft.Json.JsonConvert.DeserializeObject<GrafanaModel>(json);

                    //operazioni json
                   
                    var rules = await _rulesCollection.Find(Builders<Models.Rule>.Filter.Empty).ToListAsync();

                    //reset soglie

                    foreach (Panel panel in model.Dashboard.Panels)
                    {
                        panel.Thresholds = new List<Threshold>();
                    }


                    foreach (Models.Rule rule in rules)
                    {
                        String fieldName = rule.Field;
                        Panel tempPanel = model.Dashboard.Panels.Find(x => x.Description != null && x.Description.Equals(fieldName));


                        Threshold t = new Threshold();
                        t.ColorMode = "critical";
                        t.Fill = true;
                        t.Line = true;
                        t.Yaxis = "left";
                        t.Value = Convert.ToInt64(rule.Value);

                        switch (rule.ConditionOperator)
                        {
                            case ">":
                                t.Op = "gt";
                                break;
                            case ">=":
                                t.Op = "gt";
                                break;
                            case "<":
                                t.Op = "lt";
                                break;
                            case "<=":
                                t.Op = "lt";
                                break;
                        }

                        tempPanel.Thresholds.Add(t);
                    }



                    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpRequestMessage h = new HttpRequestMessage();
                    var jsonDashboard = Newtonsoft.Json.JsonConvert.SerializeObject(model);

                    Uri uri = new Uri($"http://10.0.0.73:3000/api/dashboards/db");
                    h.RequestUri = uri;


                    
                    h.Method = HttpMethod.Post;
                    h.Content = new StringContent(jsonDashboard);

                    h.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "eyJrIjoiaVRPQlo3ODI2aDlnQ3RwRUdEUnAyMFUxelE3Y1VZdWMiLCJuIjoiUHJvdmEiLCJpZCI6MX0=");

                    h.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpResponseMessage response = await client.SendAsync(h);
                }

                //modifico json 



                //invio json 

            }
            catch(HttpRequestException e)
            {
                log.Error($"Error: {e.Message}");
            }  
        }
    }
}
