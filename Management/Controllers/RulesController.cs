using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileM.Models;
using Management.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Utils;
using com.sun.org.apache.xpath.@internal.axes;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using log4net;
using Newtonsoft.Json;
using com.sun.xml.@internal.fastinfoset.util;
using Threshold = Management.Models.Threshold;
using RabbitMQ.Client;

namespace Management.Controllers {
    public class RulesController : Controller {
        private static IWebHostEnvironment _hostingEnvironment;
        private readonly Config _config;
        private readonly IMongoClient _client;
        private static IMongoDatabase _database;
        private readonly IMongoCollection<Models.Rule> _rulesCollection;
        static readonly HttpClient client = new HttpClient ();
        private static readonly ILog log = LogManager.GetLogger (MethodBase.GetCurrentMethod ().DeclaringType);
        private static ClientAMQP _amqpconn;
        private static TaskCompletionSource<string> taskCompletionSource;

        public IActionResult Index () {
            return View ();
        }

        public RulesController (IWebHostEnvironment hostEnvironment) {
            try {
                _hostingEnvironment = hostEnvironment;
                _config = Utils.Utils.ReadConfiguration ();

                //inizializzo connessione con MongoDB
                _client = new MongoClient (_config.MongoDB.ConnectionString);
                _database = _client.GetDatabase ("MiniIoT");
                _rulesCollection = _database.GetCollection<Models.Rule> ("Rules");

                //Inizializzazione client AMQP 
                _amqpconn = new ClientAMQP ();
                _amqpconn.CreateExchange (_config.Communications.AMQP.Exchange, ExchangeType.Direct.ToString ());

                //creo coda management
                _amqpconn.CreateQueue ("management");

                //bind coda database a exchange e routing key 'database' 
                //riservato per le comunicazioni al database (es. richieste telemetrie da parte del modulo Alerting)
                _amqpconn.BindQueue ("management", "direct_message", "management");

                //imposto evento ricezione messaggi AMQP
                _amqpconn.AMQPMessageReceived += OnAMQPMessageReceived;
                _amqpconn.ReceiveMessageAsync ("management");

                taskCompletionSource = new TaskCompletionSource<string> ();
            } catch (Exception e) {
                log.Error ($"Error: {e.Message}");
            }

        }

        public async void OnAMQPMessageReceived (object sender, String msg) {
            try {
                var message = JsonConvert.DeserializeObject<AMQPMessage> (msg);
                switch (message.Type) {
                    //Ricezione risposta query da parte del microservizio DATABASE
                    case AMQPMessageType.QueryResult:
                        var queryResultJson = message.Data;
                        var queryResult = JsonConvert.DeserializeObject<QueryResult> (queryResultJson);
                        taskCompletionSource.SetResult (queryResult.Payload);

                        break;
                }

                Console.WriteLine ("{0} - {1}: ", message.Type.ToString (), message.Data);
            } catch (Exception e) {
                log.ErrorFormat ("!ERROR: {0}", e.ToString ());
            }

        }

        public async Task<string[]> GetMachines () {
            //invio richiesta query a microservizio database
            Utils.Query q = new Utils.Query { Type = "GetMachines" };
            var message = new AMQPMessage { Type = AMQPMessageType.Query, Data = JsonConvert.SerializeObject (q), Sender = "management" };
            await _amqpconn.SendMessageAsync ("direct_message", "database", JsonConvert.SerializeObject (message));

            //ricezione query result
            string result = await taskCompletionSource.Task;
            var machines = JsonConvert.DeserializeObject<List<String>> (result).ToArray ();
            return machines;
        }

        public async Task<string[]> GetFieldsByMachines (IList<string> machines) {
            if (machines.Count > 0) {
                var machinesJson = JsonConvert.SerializeObject (machines);
                Utils.Query q = new Utils.Query { Type = "GetFieldsByMachines", Field = machinesJson };
                var message = new AMQPMessage { Type = AMQPMessageType.Query, Data = JsonConvert.SerializeObject (q), Sender = "management" };
                await _amqpconn.SendMessageAsync ("direct_message", "database", JsonConvert.SerializeObject (message));

                //ricezione query result
                string result = await taskCompletionSource.Task;
                if (String.IsNullOrEmpty (result)) {
                    return null;
                } else {
                    var fields = JsonConvert.DeserializeObject<List<String>> (result).ToArray ();
                    return fields;
                }
            } else {
                return null;
            }
        }

        public async Task<IActionResult> Modal (String mode, String id) {
            try {
                if (mode.Equals ("add")) {
                    ViewBag.mode = "add";
                    return PartialView ("_Modal");
                } else {
                    ViewBag.mode = "edit";
                    var rule = await _rulesCollection.Find (r => r.Id == id).FirstOrDefaultAsync ();
                    return PartialView ("_Modal", rule);
                }
            } catch (Exception e) {
                log.Error ($"Error: {e.Message}");
                return new EmptyResult ();
            }

        }

        public async Task<JsonResult> LoadRules (DataTableAjaxPostModel model) {
            try {
                var builder = Builders<Models.Rule>.Filter;
                String searchValue = model.search.value;
                FilterDefinition<Models.Rule> filter;

                if (!String.IsNullOrEmpty (searchValue))
                    filter = builder.Where (x => x.Machine.Contains (searchValue)) |
                    builder.Where (x => x.Name.Contains (searchValue));
                else
                    filter = builder.Empty;

                long totalCount = await _rulesCollection.CountDocumentsAsync (filter);

                var rules = await _rulesCollection.Find<Models.Rule> (filter).Skip (model.start).Limit (model.length).ToListAsync ();

                long limitedCount = rules.Count;

                return Json (new {
                    draw = model.draw,
                        recordsTotal = totalCount,
                        recordsFiltered = limitedCount,
                        data = rules
                });
            } catch (Exception e) {
                log.Error ($"Error: {e.Message}");
                return Json (new { result = false });
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> InsertRule (Models.Rule rule) {
            try {
                //inserimento su database
                await _rulesCollection.InsertOneAsync (rule);
                if (_config.Grafana.Enabled)
                    SendThreshold ();
                return Json (new { result = true });
            } catch (Exception e) {
                log.Error ($"Error: {e.Message}");
                return Json (new { result = false });
            }

        }

        public async Task<JsonResult> UpdateRule (Models.Rule rule) {
            try {
                var result = await _rulesCollection.UpdateOneAsync (r => r.Id == rule.Id, Builders<Models.Rule>.Update
                    .Set (r => r.Machine, rule.Machine)
                    .Set (r => r.Name, rule.Name)
                    .Set (r => r.Period, rule.Period)
                    .Set (r => r.Frequency, rule.Frequency)
                    .Set (r => r.Severity, rule.Severity)
                    .Set (r => r.Field, rule.Field)
                    .Set (r => r.ConditionOperator, rule.ConditionOperator)
                    .Set (r => r.Value, rule.Value)
                    .Set (r => r.actions, rule.actions));

                if (result.IsAcknowledged && result.ModifiedCount > 0) {
                    if (_config.Grafana.Enabled)
                        SendThreshold ();
                    return Json (new { result = true });
                } else
                    return Json (new { result = false });
            } catch (Exception e) {
                log.Error ($"Error: {e.Message}");
                return Json (new { result = false });
            }

        }

        public async Task<JsonResult> DeleteRule (String id) {
            try {
                var result = await _rulesCollection.DeleteOneAsync (r => r.Id == id);
                if (result.IsAcknowledged && result.DeletedCount > 0) {
                    if (_config.Grafana.Enabled)
                        SendThreshold ();
                    return Json (new { result = true });

                } else
                    return Json (new { result = false });
            } catch (Exception e) {
                log.Error ($"Error: {e.Message}");
                return Json (new { result = false });
            }
        }

        public async void SendThreshold () {
            try {
                client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue ("application/json"));

                //recupero json dashboard da Grafana (in modo tale da avere sempre la versione aggiornata)
                HttpRequestMessage h = new HttpRequestMessage ();

                var dashId = "Z72jNMSMk";

                Uri uri = new Uri ($"http://10.0.0.88:3000/api/dashboards/uid/{dashId}");
                // Uri uri = new Uri ($"http://192.168.1.120:3000/api/dashboards/uid/{dashId}");
                h.RequestUri = uri;

                h.Method = HttpMethod.Get;
                h.Headers.Authorization = new AuthenticationHeaderValue ("Bearer", "eyJrIjoiaVRPQlo3ODI2aDlnQ3RwRUdEUnAyMFUxelE3Y1VZdWMiLCJuIjoiUHJvdmEiLCJpZCI6MX0=");
                HttpResponseMessage response = await client.SendAsync (h);
                String dashboardJson = await response.Content.ReadAsStringAsync ();

                var dashboardModel = Newtonsoft.Json.JsonConvert.DeserializeObject<GrafanaModel> (dashboardJson);
                dashboardModel.Overwrite = true;

                //operazioni json

                var rules = await _rulesCollection.Find (Builders<Models.Rule>.Filter.Empty).ToListAsync ();

                //reset soglie

                foreach (Panel panel in dashboardModel.Dashboard.Panels) {
                    panel.Thresholds = new List<Threshold> ();
                }

                foreach (Models.Rule rule in rules) {
                    String fieldName = rule.Field;
                    Panel tempPanel = dashboardModel.Dashboard.Panels.Find (x => x.Description != null && x.Description.Equals (fieldName));

                    Threshold t = new Threshold ();
                    t.ColorMode = "critical";
                    t.Fill = true;
                    t.Line = true;
                    t.Yaxis = "left";
                    t.Value = Convert.ToInt64 (rule.Value);

                    switch (rule.ConditionOperator) {
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

                    tempPanel.Thresholds.Add (t);
                }

                //aggiornamento dashboard su Grafana

                dashboardJson = Newtonsoft.Json.JsonConvert.SerializeObject (dashboardModel);
                h = new HttpRequestMessage ();
                uri = new Uri ($"http://10.0.0.88:3000/api/dashboards/db");
                // uri = new Uri ($"http://192.168.1.120:3000/api/dashboards/db");
                h.RequestUri = uri;
                h.Method = HttpMethod.Post;
                h.Content = new StringContent (dashboardJson);
                h.Content.Headers.ContentType = new MediaTypeHeaderValue ("application/json");
                h.Headers.Authorization = new AuthenticationHeaderValue ("Bearer", "eyJrIjoiaVRPQlo3ODI2aDlnQ3RwRUdEUnAyMFUxelE3Y1VZdWMiLCJuIjoiUHJvdmEiLCJpZCI6MX0=");
                response = await client.SendAsync (h);
                h.Dispose ();
            } catch (Exception e) {
                log.Error ($"Error: {e.Message}");
            }
        }
    }
}