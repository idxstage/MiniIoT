using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Utils;

namespace Database
{
    public enum MessageType
    {
        Telemetria,
        Messaggio
    }
    public class Program
    {
        private static DBConnection _dbconnection;
        private static ClientAMQP _amqpconn;
        private static Config _config;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static ManualResetEvent _quitEvent = new ManualResetEvent(false);
        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, eArgs) => {
                _quitEvent.Set();
                eArgs.Cancel = true;
            };

            try
            {
                // Inizializzazione configurazione Log4Net
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

                //Inizializzazione configurazione
                _config = Utils.Utils.ReadConfiguration();

                //Inizializzazione connessione con InfluxDB
                _dbconnection = new DBConnection();

                //Inizializzazione modulo di monitoring
                Modulo modulo = _config.Monitoring.Modules.Find(x => x.Name.Contains("Database"));
                AliveServer(modulo.Ip, modulo.Port);

                //Inizializzazione client AMQP
                _amqpconn = new ClientAMQP();
                var exchange = _config.Communications.AMQP.Exchange;
                var queue = _config.Communications.AMQP.Queue;

                _amqpconn.CreateExchange(exchange, ExchangeType.Direct.ToString());

                //creo coda database
                _amqpconn.CreateQueue(queue);

                //bind coda database a exchange ed routing key 'common'
                //canale generale per la ricezione delle telemetrie da parte dell'hub
                _amqpconn.BindQueue(queue, exchange, "common");

                //bind coda database a exchange e routing key 'database' 
                //riservato per le comunicazioni al database (es. richieste telemetrie da parte del modulo Alerting)
                _amqpconn.BindQueue(queue, exchange, "database");

                //imposto evento ricezione messaggi AMQP
                _amqpconn.AMQPMessageReceived += OnAMQPMessageReceived;
                _amqpconn.ReceiveMessageAsync(queue);

                log.Info("DATABASE INIZIALIZZATO CORRETTAMENTE!");
                //Console.ReadLine(); 
                _quitEvent.WaitOne();
            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }

        }

        /// <summary>
        /// Metodo invocato ogni qual volta viene ricevuto un messaggio AMQP
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        public async static void OnAMQPMessageReceived(object sender, String msg)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<AMQPMessage>(msg);
                switch (message.Type)
                {
                    //Ricezione telemetrie istantanee da broker AMQP
                    case AMQPMessageType.Telemetry:
                        //scrittura su database InfluxDB
                        await _dbconnection.WriteData(message.Data);
                        break;
                    //Ricezione richiesta di query da parte del microservizio DATABASE
                    case AMQPMessageType.Query:
                        var query = JsonConvert.DeserializeObject<Query>(message.Data);
                        int period = query.Period;
                        var machineId = query.MachineId;
                        var req_sender = message.Sender;
                        var field = query.Field;
                        var type = query.Type;
                        var result = "";

                        switch (type)
                        {
                            case "GetMachines":
                                result = await _dbconnection.GetMachines();
                                break;
                            case "GetFieldsByMachines":
                                var machines = JsonConvert.DeserializeObject<List<String>>(field);
                                result = await _dbconnection.GetFieldsByMachine(machines);
                                break;
                            default:
                                result = await _dbconnection.ReadData(machineId, field, period);
                                break;
                        }

                        var body = new AMQPMessage { Type = AMQPMessageType.QueryResult, Data = result, Sender = _config.Communications.AMQP.Queue };
                        var json = JsonConvert.SerializeObject(body);
                        //invio risposta
                        await _amqpconn.SendMessageAsync(_config.Communications.AMQP.Exchange, req_sender, json);
                        break;
                }

                Console.WriteLine("{0} - {1}: ", message.Type.ToString(), message.Data);
            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }

        }

        /// <summary>
        /// Ping microservizio (monitoring)
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        private static async void AliveServer(string ip, int port)
        {
            try
            {
                await Task.Run(() => {
                    PingServer server = new PingServer(ip, port);
                });
            }
            catch (Exception e)
            {

                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }

        }
    }
}