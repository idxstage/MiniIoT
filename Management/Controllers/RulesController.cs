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
using MongoDB.Bson.IO;
using Newtonsoft.Json;
using System.Net.Http;

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



  
        public async Task<JsonResult> Test()
        {
            
            String json = "{\"annotations\":{\"list\":[{\"builtIn\":1,\"datasource\":\"-- Grafana --\",\"enable\":true,\"hide\":true,\"iconColor\":\"rgba(0, 211, 255, 1)\",\"name\":\"Annotations & Alerts\",\"type\":\"dashboard\"}]},\"editable\":true,\"gnetId\":null,\"graphTooltip\":0,\"id\":2,\"links\":[],\"panels\":[{\"content\":\"<h1>Stage Mini-IOT</h1>\n\n\",\"datasource\":null,\"fieldConfig\":{\"defaults\":{\"custom\":{}},\"overrides\":[]},\"gridPos\":{\"h\":2,\"w\":18,\"x\":0,\"y\":0},\"id\":12,\"mode\":\"html\",\"targets\":[{\"groupBy\":[{\"params\":[\"$__interval\"],\"type\":\"time\"},{\"params\":[\"null\"],\"type\":\"fill\"}],\"orderByTime\":\"ASC\",\"policy\":\"default\",\"refId\":\"A\",\"resultFormat\":\"time_series\",\"select\":[[{\"params\":[\"value\"],\"type\":\"field\"},{\"params\":[],\"type\":\"mean\"}]],\"tags\":[]}],\"timeFrom\":null,\"timeShift\":null,\"title\":\"\",\"transparent\":true,\"type\":\"text\"},{\"collapsed\":false,\"datasource\":null,\"gridPos\":{\"h\":1,\"w\":24,\"x\":0,\"y\":2},\"id\":10,\"panels\":[],\"title\":\"DISP_123\",\"type\":\"row\"},{\"aliasColors\":{},\"bars\":false,\"dashLength\":10,\"dashes\":false,\"datasource\":\"InfluxDB\",\"description\":\"corrente\",\"fieldConfig\":{\"defaults\":{\"custom\":{}},\"overrides\":[]},\"fill\":1,\"fillGradient\":0,\"gridPos\":{\"h\":7,\"w\":8,\"x\":0,\"y\":3},\"hiddenSeries\":false,\"id\":2,\"legend\":{\"avg\":true,\"current\":true,\"max\":true,\"min\":true,\"show\":true,\"total\":false,\"values\":true},\"lines\":true,\"linewidth\":1,\"nullPointMode\":\"null\",\"options\":{\"dataLinks\":[]},\"percentage\":false,\"pointradius\":0.5,\"points\":true,\"renderer\":\"flot\",\"seriesOverrides\":[],\"spaceLength\":10,\"stack\":false,\"steppedLine\":false,\"targets\":[{\"groupBy\":[{\"params\":[\"$__interval\"],\"type\":\"time\"},{\"params\":[\"null\"],\"type\":\"fill\"}],\"measurement\":\"DISP_123\",\"orderByTime\":\"ASC\",\"policy\":\"autogen\",\"refId\":\"A\",\"resultFormat\":\"time_series\",\"select\":[[{\"params\":[\"corrente\"],\"type\":\"field\"},{\"params\":[],\"type\":\"mean\"}]],\"tags\":[]}],\"thresholds\":[],\"timeFrom\":null,\"timeRegions\":[],\"timeShift\":null,\"title\":\"Corrente [A]\",\"tooltip\":{\"shared\":true,\"sort\":0,\"value_type\":\"individual\"},\"transparent\":true,\"type\":\"graph\",\"xaxis\":{\"buckets\":null,\"mode\":\"time\",\"name\":null,\"show\":true,\"values\":[]},\"yaxes\":[{\"format\":\"short\",\"label\":\"[A]\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true},{\"format\":\"short\",\"label\":\"\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true}],\"yaxis\":{\"align\":false,\"alignLevel\":null}},{\"aliasColors\":{},\"bars\":false,\"dashLength\":10,\"dashes\":false,\"datasource\":\"InfluxDB\",\"description\":\"potenza_assorbita\",\"fieldConfig\":{\"defaults\":{\"custom\":{}},\"overrides\":[]},\"fill\":1,\"fillGradient\":0,\"gridPos\":{\"h\":7,\"w\":8,\"x\":8,\"y\":3},\"hiddenSeries\":false,\"id\":6,\"legend\":{\"avg\":true,\"current\":true,\"max\":true,\"min\":true,\"show\":true,\"total\":false,\"values\":true},\"lines\":true,\"linewidth\":1,\"nullPointMode\":\"null\",\"options\":{\"dataLinks\":[]},\"percentage\":false,\"pointradius\":0.5,\"points\":true,\"renderer\":\"flot\",\"seriesOverrides\":[],\"spaceLength\":10,\"stack\":false,\"steppedLine\":false,\"targets\":[{\"groupBy\":[{\"params\":[\"$__interval\"],\"type\":\"time\"},{\"params\":[\"null\"],\"type\":\"fill\"}],\"measurement\":\"DISP_123\",\"orderByTime\":\"ASC\",\"policy\":\"autogen\",\"refId\":\"A\",\"resultFormat\":\"time_series\",\"select\":[[{\"params\":[\"potenza_assorbita\"],\"type\":\"field\"},{\"params\":[],\"type\":\"mean\"}]],\"tags\":[]}],\"thresholds\":[],\"timeFrom\":null,\"timeRegions\":[],\"timeShift\":null,\"title\":\"Potenza assorbita [W]\",\"tooltip\":{\"shared\":true,\"sort\":0,\"value_type\":\"individual\"},\"transparent\":true,\"type\":\"graph\",\"xaxis\":{\"buckets\":null,\"mode\":\"time\",\"name\":null,\"show\":true,\"values\":[]},\"yaxes\":[{\"format\":\"short\",\"label\":\"[W]\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true},{\"format\":\"short\",\"label\":\"\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true}],\"yaxis\":{\"align\":false,\"alignLevel\":null}},{\"aliasColors\":{},\"bars\":false,\"dashLength\":10,\"dashes\":false,\"datasource\":\"InfluxDB\",\"description\":\"umidita\",\"fieldConfig\":{\"defaults\":{\"custom\":{}},\"overrides\":[]},\"fill\":1,\"fillGradient\":0,\"gridPos\":{\"h\":7,\"w\":8,\"x\":16,\"y\":3},\"hiddenSeries\":false,\"id\":4,\"legend\":{\"avg\":true,\"current\":true,\"max\":true,\"min\":true,\"show\":true,\"total\":false,\"values\":true},\"lines\":true,\"linewidth\":1,\"nullPointMode\":\"null\",\"options\":{\"dataLinks\":[]},\"percentage\":false,\"pointradius\":0.5,\"points\":true,\"renderer\":\"flot\",\"seriesOverrides\":[],\"spaceLength\":10,\"stack\":false,\"steppedLine\":false,\"targets\":[{\"groupBy\":[{\"params\":[\"$__interval\"],\"type\":\"time\"},{\"params\":[\"null\"],\"type\":\"fill\"}],\"measurement\":\"DISP_123\",\"orderByTime\":\"ASC\",\"policy\":\"autogen\",\"refId\":\"A\",\"resultFormat\":\"time_series\",\"select\":[[{\"params\":[\"umidita\"],\"type\":\"field\"},{\"params\":[],\"type\":\"mean\"}]],\"tags\":[]}],\"thresholds\":[],\"timeFrom\":null,\"timeRegions\":[],\"timeShift\":null,\"title\":\"Umidit\u00e0 [%]\",\"tooltip\":{\"shared\":true,\"sort\":0,\"value_type\":\"individual\"},\"transparent\":true,\"type\":\"graph\",\"xaxis\":{\"buckets\":null,\"mode\":\"time\",\"name\":null,\"show\":true,\"values\":[]},\"yaxes\":[{\"format\":\"short\",\"label\":\"[%]\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true},{\"format\":\"short\",\"label\":\"\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true}],\"yaxis\":{\"align\":false,\"alignLevel\":null}},{\"aliasColors\":{},\"bars\":false,\"dashLength\":10,\"dashes\":false,\"datasource\":\"InfluxDB\",\"description\":\"temperatura_esterna\",\"fieldConfig\":{\"defaults\":{\"custom\":{}},\"overrides\":[]},\"fill\":1,\"fillGradient\":0,\"gridPos\":{\"h\":7,\"w\":8,\"x\":0,\"y\":10},\"hiddenSeries\":false,\"id\":5,\"legend\":{\"avg\":true,\"current\":true,\"max\":true,\"min\":true,\"show\":true,\"total\":false,\"values\":true},\"lines\":true,\"linewidth\":1,\"nullPointMode\":\"null\",\"options\":{\"dataLinks\":[]},\"percentage\":false,\"pointradius\":0.5,\"points\":true,\"renderer\":\"flot\",\"seriesOverrides\":[],\"spaceLength\":10,\"stack\":false,\"steppedLine\":false,\"targets\":[{\"groupBy\":[{\"params\":[\"$__interval\"],\"type\":\"time\"},{\"params\":[\"null\"],\"type\":\"fill\"}],\"measurement\":\"DISP_123\",\"orderByTime\":\"ASC\",\"policy\":\"autogen\",\"refId\":\"A\",\"resultFormat\":\"time_series\",\"select\":[[{\"params\":[\"temperatura_esterna\"],\"type\":\"field\"},{\"params\":[],\"type\":\"mean\"}]],\"tags\":[]}],\"thresholds\":[],\"timeFrom\":null,\"timeRegions\":[],\"timeShift\":null,\"title\":\"Temperatura esterna [\u00b0C]\",\"tooltip\":{\"shared\":true,\"sort\":0,\"value_type\":\"individual\"},\"transparent\":true,\"type\":\"graph\",\"xaxis\":{\"buckets\":null,\"mode\":\"time\",\"name\":null,\"show\":true,\"values\":[]},\"yaxes\":[{\"format\":\"short\",\"label\":\"[\u00b0C]\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true},{\"format\":\"short\",\"label\":\"\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true}],\"yaxis\":{\"align\":false,\"alignLevel\":null}},{\"aliasColors\":{},\"bars\":false,\"dashLength\":10,\"dashes\":false,\"datasource\":\"InfluxDB\",\"description\":\"temperatura_interna\",\"fieldConfig\":{\"defaults\":{\"custom\":{}},\"overrides\":[]},\"fill\":1,\"fillGradient\":0,\"gridPos\":{\"h\":7,\"w\":8,\"x\":8,\"y\":10},\"hiddenSeries\":false,\"id\":8,\"legend\":{\"avg\":true,\"current\":true,\"max\":true,\"min\":true,\"show\":true,\"total\":false,\"values\":true},\"lines\":true,\"linewidth\":1,\"nullPointMode\":\"null\",\"options\":{\"dataLinks\":[]},\"percentage\":false,\"pointradius\":0.5,\"points\":true,\"renderer\":\"flot\",\"seriesOverrides\":[],\"spaceLength\":10,\"stack\":false,\"steppedLine\":false,\"targets\":[{\"groupBy\":[{\"params\":[\"$__interval\"],\"type\":\"time\"},{\"params\":[\"null\"],\"type\":\"fill\"}],\"measurement\":\"DISP_123\",\"orderByTime\":\"ASC\",\"policy\":\"autogen\",\"refId\":\"A\",\"resultFormat\":\"time_series\",\"select\":[[{\"params\":[\"temperatura_interna\"],\"type\":\"field\"},{\"params\":[],\"type\":\"mean\"}]],\"tags\":[]}],\"thresholds\":[],\"timeFrom\":null,\"timeRegions\":[],\"timeShift\":null,\"title\":\"Temperatura interna [\u00b0C]\",\"tooltip\":{\"shared\":true,\"sort\":0,\"value_type\":\"individual\"},\"transparent\":true,\"type\":\"graph\",\"xaxis\":{\"buckets\":null,\"mode\":\"time\",\"name\":null,\"show\":true,\"values\":[]},\"yaxes\":[{\"format\":\"short\",\"label\":\"[A]\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true},{\"format\":\"short\",\"label\":\"\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true}],\"yaxis\":{\"align\":false,\"alignLevel\":null}},{\"aliasColors\":{},\"bars\":false,\"dashLength\":10,\"dashes\":false,\"datasource\":\"InfluxDB\",\"description\":\"tensione_entrata\",\"fieldConfig\":{\"defaults\":{\"custom\":{}},\"overrides\":[]},\"fill\":1,\"fillGradient\":0,\"gridPos\":{\"h\":7,\"w\":8,\"x\":16,\"y\":10},\"hiddenSeries\":false,\"id\":7,\"legend\":{\"avg\":true,\"current\":true,\"max\":true,\"min\":true,\"show\":true,\"total\":false,\"values\":true},\"lines\":true,\"linewidth\":1,\"nullPointMode\":\"null\",\"options\":{\"dataLinks\":[]},\"percentage\":false,\"pointradius\":0.5,\"points\":true,\"renderer\":\"flot\",\"seriesOverrides\":[],\"spaceLength\":10,\"stack\":false,\"steppedLine\":false,\"targets\":[{\"groupBy\":[{\"params\":[\"$__interval\"],\"type\":\"time\"},{\"params\":[\"null\"],\"type\":\"fill\"}],\"measurement\":\"DISP_123\",\"orderByTime\":\"ASC\",\"policy\":\"autogen\",\"refId\":\"A\",\"resultFormat\":\"time_series\",\"select\":[[{\"params\":[\"tensione_entrata\"],\"type\":\"field\"},{\"params\":[],\"type\":\"mean\"}]],\"tags\":[]}],\"thresholds\":[],\"timeFrom\":null,\"timeRegions\":[],\"timeShift\":null,\"title\":\"Tensione entrata [V]\",\"tooltip\":{\"shared\":true,\"sort\":0,\"value_type\":\"individual\"},\"transparent\":true,\"type\":\"graph\",\"xaxis\":{\"buckets\":null,\"mode\":\"time\",\"name\":null,\"show\":true,\"values\":[]},\"yaxes\":[{\"format\":\"short\",\"label\":\"[V]\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true},{\"format\":\"short\",\"label\":\"\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true}],\"yaxis\":{\"align\":false,\"alignLevel\":null}},{\"aliasColors\":{},\"bars\":false,\"dashLength\":10,\"dashes\":false,\"datasource\":\"InfluxDB\",\"description\":\"tensione_lampadina\",\"fieldConfig\":{\"defaults\":{\"custom\":{}},\"overrides\":[]},\"fill\":1,\"fillGradient\":0,\"gridPos\":{\"h\":7,\"w\":8,\"x\":0,\"y\":17},\"hiddenSeries\":false,\"id\":3,\"legend\":{\"avg\":true,\"current\":true,\"max\":true,\"min\":true,\"show\":true,\"total\":false,\"values\":true},\"lines\":true,\"linewidth\":1,\"nullPointMode\":\"null\",\"options\":{\"dataLinks\":[]},\"percentage\":false,\"pointradius\":0.5,\"points\":true,\"renderer\":\"flot\",\"seriesOverrides\":[],\"spaceLength\":10,\"stack\":false,\"steppedLine\":false,\"targets\":[{\"groupBy\":[{\"params\":[\"$__interval\"],\"type\":\"time\"},{\"params\":[\"null\"],\"type\":\"fill\"}],\"measurement\":\"DISP_123\",\"orderByTime\":\"ASC\",\"policy\":\"autogen\",\"refId\":\"A\",\"resultFormat\":\"time_series\",\"select\":[[{\"params\":[\"tensione_lampadina\"],\"type\":\"field\"},{\"params\":[],\"type\":\"mean\"}]],\"tags\":[]}],\"thresholds\":[],\"timeFrom\":null,\"timeRegions\":[],\"timeShift\":null,\"title\":\"Tensione lampadina [V]\",\"tooltip\":{\"shared\":true,\"sort\":0,\"value_type\":\"individual\"},\"transparent\":true,\"type\":\"graph\",\"xaxis\":{\"buckets\":null,\"mode\":\"time\",\"name\":null,\"show\":true,\"values\":[]},\"yaxes\":[{\"format\":\"short\",\"label\":\"[V]\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true},{\"format\":\"short\",\"label\":\"\",\"logBase\":1,\"max\":null,\"min\":null,\"show\":true}],\"yaxis\":{\"align\":false,\"alignLevel\":null}}],\"refresh\":false,\"schemaVersion\":25,\"style\":\"dark\",\"tags\":[],\"templating\":{\"list\":[]},\"time\":{\"from\":\"now-6h\",\"to\":\"now\"},\"timepicker\":{\"refresh_intervals\":[\"10s\",\"30s\",\"1m\",\"5m\",\"15m\",\"30m\",\"1h\",\"2h\",\"1d\"]},\"timezone\":\"\",\"title\":\"MiniIoT\",\"uid\":\"Z72jNMSMk\",\"version\":4}";

            var amb = Newtonsoft.Json.JsonConvert.DeserializeObject<GrafanaModel>(json);

            return null;

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


    }
}
