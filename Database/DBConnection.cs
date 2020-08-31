using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Flux;
using InfluxDB.Client.Writes;
using log4net;
using MQTTnet.Client.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Utils;

namespace Database
{

    class DBConnection
    {
        private readonly InfluxDBClient client = null;
        private readonly string retentionPolicy;
        private readonly string database;
        private static readonly ILog log = LogManager.GetLogger(typeof(DBConnection));
        public DBConnection()
        {
            try
            {
                //lettura configurazione
                Config config = Utils.Utils.ReadConfiguration();
                String connString = "";
                if (config != null)
                {
                    connString = String.Format("http://{0}:{1}", config.InfluxDB.Ip, config.InfluxDB.Port);
                    //creazione client per connessione al db
                    database = config.InfluxDB.Database;
                    retentionPolicy = config.InfluxDB.RetentionPolicy;
                    string username = config.InfluxDB.Username;
                    char[] password = config.InfluxDB.Password.ToCharArray();
                    client = InfluxDBClientFactory.CreateV1(connString, username, password, database, retentionPolicy);
                }
                if (client == null)
                    throw new Exception("Impossibile stabilire connessione con il database!");
            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }

        }
        ~DBConnection()
        {
            try
            {
                if (client != null) client.Dispose();
            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }

        }

        /// <summary>
        /// Lettura telemetrie da database
        /// </summary>
        /// <param name="machine_id">Identificativo della macchina</param>
        /// <param name="field">Field di cui ritornare i valori</param>
        /// <param name="period">Intervallo temporale a partire dall'istante corrente a ritroso di cui estrarre le telemetrie</param>
        /// <returns></returns>
        public async Task<String> ReadData(String machine_id, String field, int period = 0)
        {

            String json = "";
            long elapsedMs = 0;
            var res = new QueryResult();

            try
            {
                var query = $"from(bucket:\"{database}\") " +
                    $"|> range(start: -{period}s) " +
                    $"|> filter(fn: (r) => r._measurement == \"{machine_id}\" and r._field == \"{field}\")";

                //esecuzione query
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var fluxTables = await client.GetQueryApi().QueryAsync(query);
                watch.Stop();
                elapsedMs = watch.ElapsedMilliseconds;

                //se ci sono risultati ( se count = 0  =>  non ci sono risultati)
                if (fluxTables != null && fluxTables.Count > 0)
                {

                    //Nota: InfluxDB restituisce i risultati come lista di colonne

                    //numero di colonne dei risultati
                    int countColumns = fluxTables.Count;
                    //numero di righe dei risultati
                    int countRecords = fluxTables[0].Records.Count;
                    Dictionary<String, Object>[] values = new Dictionary<string, object>[countRecords];

                    //inizializzazione
                    for (int i = 0; i < countRecords; i++)
                        values[i] = new Dictionary<string, object>();

                    //aggiungo timestamp ad ogni entry
                    for (int j = 0; j < countRecords; j++)
                    {
                        var timestamp = fluxTables[0].Records[j].GetTime().GetValueOrDefault().ToUnixTimeMilliseconds();
                        values[j].Add("ts", timestamp);
                    }

                    //costruzione delle righe
                    for (int i = 0; i < countColumns; i++)
                    {
                        for (int j = 0; j < countRecords; j++)
                        {
                            var temp = fluxTables[i].Records[j];
                            String key = fluxTables[i].Records[j].GetField();
                            object value = fluxTables[i].Records[j].GetValue();
                            values[j].Add(key, value);
                        }
                    }
                    //conversione in stringa json 
                    json = JsonConvert.SerializeObject(values);
                }

                if (fluxTables == null)
                    res.Result = false;
                else
                    res.Result = true;
            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
                res.Result = false;
            }

            res.Time = elapsedMs;
            res.Payload = json;
            res.MachineId = machine_id;
            return JsonConvert.SerializeObject(res);
        }

        public async Task<string> GetMachines()
        {
            var query = $"import \"influxdata/influxdb/v1\" " +
                $"        v1.measurements(bucket: \"{database}\")";

            List<object> records = null;
            var fluxTables = await client.GetQueryApi().QueryAsync(query);
            foreach (var table in fluxTables)
            {
                records = table.Records.Select(r => r.GetValue()).ToList();
            }

            var payloadJson = JsonConvert.SerializeObject(records);
            var result = new QueryResult { Payload = payloadJson };
            return JsonConvert.SerializeObject(result);
        }

        public async Task<string> GetFieldsByMachine(List<String> machines)
        {

            List<object> records = new List<object>();
            bool isFirst = true;
            foreach (var machine in machines)
            {
                var query = $"import \"influxdata/influxdb/v1\" " +
                $"        v1.measurementTagValues(bucket: \"{database}\", measurement: \"{machine}\", tag: \"_field\")";
                var fluxTables = await client.GetQueryApi().QueryAsync(query);
                if (fluxTables.Count > 0)
                {
                    var tempRecords = fluxTables[0].Records.Select(r => r.GetValue()).ToList();
                    if (isFirst)
                    {
                        records = tempRecords;
                        isFirst = false;
                    }
                    else
                    {
                        records.Intersect(tempRecords);
                    }
                }

            }

            var payloadJson = JsonConvert.SerializeObject(records);
            var result = new QueryResult { Payload = payloadJson };
            return JsonConvert.SerializeObject(result);

        }

        /// <summary>
        /// Scrittura telemetria su database
        /// </summary>
        /// <param name="jsonTelemetry">Telemetria in formato json</param>
        /// <returns></returns>
        public async Task WriteData(String jsonTelemetry)
        {
            try
            {
                //deserializzazione stringa json in dizionario 
                Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonTelemetry);

                using (var writeApi = client.GetWriteApi())
                {
                    //ottengo id della macchina a partire dalla telemetria
                    var machine_id = values.First(u => u.Key.Equals("machine_id"));

                    //COSTRUZIONE NUOVA ENTRY (POINT)
                    //Questa entry verrà inserita nella serie (tabella/measurement) denominata dal'identificativo 
                    //della macchina (machine_id)

                    var point = PointData.Measurement(machine_id.Value.ToString());
                    values.Remove(machine_id.Key);

                    foreach (KeyValuePair<string, object> kv in values)
                    {
                        if (kv.Key == "ts")
                            point = point.Timestamp(Convert.ToInt64(kv.Value), WritePrecision.Ms);
                        else
                            point = point.Field(kv.Key, Convert.ToInt64(kv.Value));
                    }

                    //scrittura su db
                    writeApi.WritePoint(point);
                }
            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }
        }
    }
}