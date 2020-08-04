using Database;
using log4net;
using log4net.Config;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;
namespace Hub
{
    class Program
    {
        private static ClientAMQP _amqpconn;
        private static Config _config;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        static void Main()
        {

            try
            {


                // Inizializzazione configurazione Log4Net
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
                                
                _config = Utils.Utils.ReadConfiguration();
                Modulo modulo = _config.Monitoring.Modules.Find(x => x.Name.Contains("Hub"));
                AliveServer(modulo.Ip, modulo.Port);

                _amqpconn = new ClientAMQP();
                _amqpconn.CreateExchange(_config.Communications.AMQP.Exchange, "direct");

                //lettura da NodeRed via MQTT       
                ServerMQTT mqttC = new ServerMQTT();
                mqttC.MessageReceived += OnMQTTMessageReceived;
                mqttC.StartAsync();
                mqttC.ReceiveAsync();
                Console.ReadLine();
                                 
            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }            
        }

        private static async void OnMQTTMessageReceived(object sender, string e)
        {
            //scrittura su broker AMQP
            try
            {
                var message = new AMQPMessage { Data = e, Type = AMQPMessageType.Telemetry, Sender = _config.Communications.AMQP.Queue };
                var json = JsonConvert.SerializeObject(message);
                await _amqpconn.SendMessageAsync(_config.Communications.AMQP.Exchange, "common" ,json);             
                
            }
            catch (Exception ex)
            {
                log.ErrorFormat("!ERROR: {0}", ex.ToString());                
            }                  
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
