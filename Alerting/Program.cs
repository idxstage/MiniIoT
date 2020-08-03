using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Utils;
using log4net;
using log4net.Config;
using System.Collections;
using Alerting.Model;
using Action = Alerting.Model.Action;
using System.Threading.Tasks;
using System.Text;
using RabbitMQ.Client.Events;
using Database;
using System.Reflection.PortableExecutable;
using System.Threading;
using InfluxDB.Client.Api.Domain;
using Query = Utils.Query;
using System.Linq;
using System.Net.WebSockets;
using MongoDB.Driver;


namespace Alerting
{
    class Program
    {
        static Rules rules = new Rules();
        private static ClientAMQP _amqpconn;
        private static Config _config;
        private static Semaphore _pool;
        private static IMongoClient _client;
        private static IMongoDatabase _database;
        private static IMongoCollection<Rule> _rulesCollection;
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private static Timer refreshRule;

        static void Main(string[] args)
        {
            _amqpconn = new ClientAMQP();
            _config = Utils.Utils.ReadConfiguration();

            Modulo modulo = _config.Monitoring.Modules.Find(x => x.Name.Contains("Alerting"));
            AliveServer(modulo.Ip, modulo.Port);

            var exchange = _config.Communications.AMQP.Exchange;
            var queue = _config.Communications.AMQP.Queue;

            _amqpconn.CreateExchange(exchange, "direct");

            //creo coda database
            _amqpconn.CreateQueue(queue);

            //bind coda database a exchange ed routing key 'common'
            //canale generale per la ricezione delle telemetrie da parte dell'hub
            _amqpconn.BindQueue(queue, exchange, "common");

            //bind coda database a exchange e routing key 'database' 
            //riservato per le comunicazioni al modulo alerting (es. risposta query al modulo Dabase)
            _amqpconn.BindQueue(queue, exchange, "alerting");

            //imposto evento ricezione messaggi AMQP
            _amqpconn.AMQPMessageReceived += OnAMQPMessageReceived;
            _amqpconn.ReceiveMessageAsync(queue);

            //inizializzo connessione con MongoDB
            _client = new MongoClient(_config.MongoDB.ConnectionString);
            _database = _client.GetDatabase("MiniIoT");
            _rulesCollection = _database.GetCollection<Rule>("Rules");

            var currentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var filePath = $"{currentPath}\\rules.json";

            LoadRules(filePath);

            refreshRule = new Timer(StartTasksToDB, null, 0, 10000);
            // GetTelemetryFromDB("DISP_123", 3600 * 24 * 3,"tensione_entrata");

            // GetTelemetryFromDB("DISP_123", 3600 * 24 * 3,"tensione_entrata");
            //StartTasksToDB();

            Console.ReadLine();
        }


        public static async void GetTelemetryFromDB(string machineId, int period, string field)
        {
            var query = new Query();
            query.MachineId = machineId;
            query.Period = period;
            query.Field = field;
            var data = JsonConvert.SerializeObject(query);
            var request = new AMQPMessage();
            request.Sender = _config.Communications.AMQP.Queue;
            request.Data = data;
            request.Type = AMQPMessageType.Query;

            var json = JsonConvert.SerializeObject(request);


            var _amqpconn = new ClientAMQP();
            var exchange = _config.Communications.AMQP.Exchange;
            var queue = _config.Communications.AMQP.Queue;

            _amqpconn.CreateExchange(exchange, "direct");

            //creo coda database
            _amqpconn.CreateQueue(queue);
            //bind coda database a exchange e routing key 'database' 
            //riservato per le comunicazioni al modulo alerting (es. risposta query al modulo Dabase)
            _amqpconn.BindQueue(queue, exchange, "alerting");

            await _amqpconn.SendMessageAsync(_config.Communications.AMQP.Exchange, "database", json);

        }

        public async static void OnAMQPMessageReceived(object sender, String msg)
        {

            var message = JsonConvert.DeserializeObject<AMQPMessage>(msg);
            switch (message.Type)
            {
                case AMQPMessageType.Telemetry:
                    await Task.Run(() =>
                    {
                        CheckRules(message.Data);
                    });

                    break;
                case AMQPMessageType.QueryResult:
                    await Task.Run(() =>
                    {
                        Console.WriteLine("Risposta: " + message.Data);
                        var result = JsonConvert.DeserializeObject<QueryResult>(message.Data);

                        List<Dictionary<string, string>> telemetrie = new List<Dictionary<string, string>>();
                        telemetrie = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(result.Payload);


                        double somma = 0;
                        List<string> indici = null;
                        List<string> valori = null;
                        bool b = false; // serve per vedere se ts è il primo o secondo dato
                        int i = -1;
                        Dictionary<string, string> media = new Dictionary<string, string>();
                        Dictionary<string, string> min = new Dictionary<string, string>();
                        Dictionary<string, string> max = new Dictionary<string, string>();
                        int m = Int32.MaxValue, M = -1; // m è minimo e M è massimo
                        var k = telemetrie.GetEnumerator();

                        while (k.MoveNext())
                        {
                            // otteniamo i campi della telemetria
                            string field = null;
                            indici = k.Current.Keys.ToList<string>();
                            valori = k.Current.Values.ToList<string>();


                            if (indici.IndexOf("ts") == 0)
                            {
                                if (!b)
                                {
                                    b = true;
                                    i = 1;
                                }
                                field = valori[1];
                            }
                            else
                            {
                                if (!b)
                                {
                                    b = true;
                                    i = 0;
                                }
                                field = valori[0];
                            }

                            somma += Convert.ToDouble(field);

                            
                            if (Convert.ToInt32(field) > M)
                                M = Convert.ToInt32(field);
                            if (Convert.ToInt32(field) < m)
                                m = Convert.ToInt32(field);
                        }

                        double med = somma / telemetrie.Count;
                        // media
                        media.Add("machine_id", result.MachineId);
                        media.Add(indici[i], med.ToString());
                        media.Add("type_telemetry", "Average");

                        // min 
                        min.Add("machine_id", result.MachineId);
                        min.Add(indici[i], m.ToString());
                        min.Add("type_telemetry", "Minimun");

                        // max
                        max.Add("machine_id", result.MachineId);
                        max.Add(indici[i], M.ToString());
                        max.Add("type_telemetry", "Maximum");

                        // controllo regole
                        CheckRules(media);
                        CheckRules(min);
                        CheckRules(max);

                    });
                    break;
            }

            Console.WriteLine("Messaggio: " + message.Data);
        }

        /// <summary>
        /// Metodo che inizializza tutte le richieste temporizzate al DB
        /// </summary>
        /// 
        static List<Timer> listaThread = new List<Timer>();
        private static void StartTasksToDB(object o)
        {
            // ricerchiamo tra le regole quelle che hanno Period e Frequency non a null
            // Frequency dice ogni quanto chiediamo al db le telemetrie, period quanto vecchie

            Rules regoleValide = new Rules();
            regoleValide.rules = new List<Rule>();
            listaThread = new List<Timer>();

            foreach (Rule r in rules.rules)
            {
                if (r.Period != null && r.Frequency != null)
                    regoleValide.rules.Add(r);
            }


            // otteniamo la telemetria per ogni macchina per quel periodo
            foreach (Rule regola in regoleValide.rules)
            {
                foreach (string m in regola.Machine)
                {
                    string b = $"{m};{regola.Period};{regola.Field}";
                    // il task sia avvia subito e con una certa frequenza definita da regola.Frequency
                    Timer t = new Timer(RequestQuery, b, 0, (int)regola.Frequency * 1000);
                    listaThread.Add(t);
                }
            }
        }

        /// <summary>
        /// Metodo che avvia tutte le richieste al DB
        /// </summary>
        /// <param name="body"></param>
        static void RequestQuery(object body)
        {
            string b = (string)body;
            string id = b.Split(';')[0];
            int period = Convert.ToInt32(b.Split(';')[1]);
            string field = b.Split(';')[2];
            GetTelemetryFromDB(id, period, field);
        }

        private static Dictionary<string, string> SmontaTelemetria(string telemetria)
        {
            try
            {
                telemetria = telemetria.Replace("{", "").Replace("}", "").Trim(); // rimuoviamo le graffe e spazi vari 
                Dictionary<string, string> campiTele = new Dictionary<string, string>();

                string[] variabili = telemetria.Split(','); // separiamo le variabili e i valori
                foreach (string s in variabili) // puliamo le stringhe
                {
                    string k = s.Split(":")[0].Trim();
                    k = k.Replace("\\", "").Replace("\"", "");
                    string value = s.Split(":")[1].Trim();
                    value = value.Replace("\\", "").Replace("\"", "");
                    campiTele.Add(k, value);
                }

                return campiTele;
            }
            catch(Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
                return null;
            }
            
        }


        /// <summary>
        /// Metodo che gestisce la telemetria e controlla i parametri
        /// </summary>
        /// <param name="telemetria"></param>

        private async static void CheckRules(string telemetria)
        {
            Dictionary<string, string> campiTele = SmontaTelemetria(telemetria);

            #region Controllo Telemetria

            string machine;
            // controlliamo che la macchina sia contenuta all'interno della lista

            if (campiTele.ContainsKey("machine_id") && campiTele.TryGetValue("machine_id", out machine))
            {
                    var rules = await GetRulesFromDB(machine);
                
                    

                    //per ogni rule nel batch
                    foreach (var r in rules)
                    {
                        if (r.Machine.Contains(machine)) // applichiamo la regola a quella macchina
                        {
                            // controlliamo se qualche campo della telemetria è contenuta nelle regole
                            if (campiTele.ContainsKey(r.Field))
                            {
                                // otteniamo il campo value
                                string v;
                                campiTele.TryGetValue(r.Field, out v);

                                double value = Convert.ToDouble(v);
                                double rValue = Convert.ToDouble(r.Value);

                                value = Math.Round(value, 3);

                                if (r.Period == null && r.Frequency == null) // made from instantanea
                                {
                                    if (!campiTele.TryAdd("type_telemetry", "Instant"))
                                        campiTele["type_telemetry"] = "Instant";
                                }


                                switch (r.ConditionOperator)
                                {
                                    case ">=":
                                        if (value >= rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                    case "<=":
                                        if (value <= rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                    case ">":
                                        if (value > rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                    case "<":
                                        if (value < rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                    case "=":
                                        if (value == rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                    case "!=":
                                        if (value != rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                }
                            }
                        }
                    }
                
            }
        }
        #endregion



        private static async Task<List<Rule>> GetRulesFromDB(String id)
        {
            return await _rulesCollection.Find(r => r.Id == id).ToListAsync();
        }

        private async static void CheckRules(Dictionary<string, string> campiTele)
        {
            #region Controllo Telemetria

            string machine;
            // controlliamo che la macchina sia contenuta all'interno della lista

            if (campiTele.ContainsKey("machine_id") && campiTele.TryGetValue("machine_id", out machine))
            {
                    var rules = await GetRulesFromDB(machine);
                
                    //per ogni rule nel batch
                    foreach (var r in rules)
                    {
                        if (r.Machine.Contains(machine)) // applichiamo la regola a quella macchina
                        {
                            // controlliamo se qualche campo della telemetria è contenuta nelle regole
                            if (campiTele.ContainsKey(r.Field))
                            {
                                // otteniamo il campo value
                                string v;
                                campiTele.TryGetValue(r.Field, out v);

                                double value = Convert.ToDouble(v);
                                double rValue = Convert.ToDouble(r.Value);

                                value = Math.Round(value, 3);

                                if (r.Period == null && r.Frequency == null) // made from instantanea
                                {
                                    if (!campiTele.TryAdd("type_telemetry", "Instant"))
                                        campiTele["type_telemetry"] = "Instant";
                                }
                                else
                                    if (!campiTele.TryAdd("type_telemetry", "Average"))
                                    campiTele["type_telemetry"] = "Average";


                                switch (r.ConditionOperator)
                                {
                                    case ">=":
                                        if (value >= rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                    case "<=":
                                        if (value <= rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                    case ">":
                                        if (value > rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                    case "<":
                                        if (value < rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                    case "=":
                                        if (value == rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                    case "!=":
                                        if (value != rValue)
                                            GetActions(r.actions, campiTele, r);
                                        break;
                                }
                            }

                        }
                    }
                


            }
            #endregion
        }

        /// <summary>
        /// Metodo che avvia tutte le azioni
        /// </summary>
        /// <param name="actions"></param>
        private static void GetActions(List<Model.Action> actions, Dictionary<string, string> campiTele, Rule r)
        {
            foreach (Action a in actions)
            {
                switch (a.type)
                {
                    case "Mail":
                        Task.Run(() =>
                        {
                            Communications.SendMessageSMTP(a, r, campiTele);
                        });
                        break;

                    case "Slack":
                        Task.Run(() =>
                        {
                            string text = "";
                            IDictionaryEnumerator k = campiTele.GetEnumerator();

                            while (k.MoveNext())
                            {
                                text += $"\n{k.Key}: {k.Value}";
                            }

                            // alleghiamo la regola
                            text += "\nRegola:\n";
                            text += $"Id: {r.Id}";
                            text += $"\nDescription: {r.Description}";
                            text += $"\nConditionOperator: {r.ConditionOperator}";
                            text += $"\nField: {r.Field}";
                            text += $"\nFrequency: {r.Frequency}";
                            text += $"\nPeriod: {r.Period}";
                            text += $"\nValue: {r.Value}";
                            foreach (string s in r.Machine)
                                text += $"\nMachine: {s}";
                            foreach (Action ac in r.actions)
                            {
                                text += $"Action\n";
                                text += $"\nType: {ac.type}";
                                text += $"\nAddress: {ac.address}";
                                text += $"\nBody: {ac.body}";
                            }

                            string value;
                            campiTele.TryGetValue("Value", out value);
                            // alleghiamo il motivo dell'email
                            text += "\nValore out poichè:\n " + r.Field + r.ConditionOperator + r.Value;

                            // alleghiamo le operazioni da fare
                            text += "\n\nOperation to do\n" + a.body;

                            Communications.SendMessageSlack(text, a.address);

                        });
                        break;
                }
            }
        }

        /// <summary>
        /// Metodo che carica le regole dato un percorso
        /// </summary>
        /// <param name="path"></param>
        /// 
        private static void LoadRules(string path)
        {
            StreamReader sr = new StreamReader(path);
            string s = sr.ReadToEnd();
            rules = JsonConvert.DeserializeObject<Rules>(s);
        }

        private static async void AliveServer(string ip, int port)
        {
            await Task.Run(() =>
            {
                PingServer server = new PingServer(ip, port);
            });
        }

    }
}
