using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace Utils
{
    

    public class ServerMQTT
    {
        public EventHandler<String> MessageReceived;             
        private readonly IMqttServer _mqttServer;
        private readonly int port;        
        private readonly List<User> users;
        private static readonly ILog log = LogManager.GetLogger(typeof(ServerMQTT));
        
        public ServerMQTT()
        {
            Config config = Utils.ReadConfiguration();          
            _mqttServer = new MqttFactory().CreateMqttServer();
            port = config.Communications.MQTT.Port;           
            users = config.Communications.MQTT.Users;
            //XmlConfigurator.Configure(Utils.GetLog4NetRepository());
        }

        protected virtual void OnMQTTMessageReceived(String telemetria)
        {
            if (MessageReceived != null)
                MessageReceived(this, telemetria);
        }        
    
        public async void StartAsync()
        {       
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint().WithDefaultEndpointPort(port).WithConnectionBacklog(100).WithConnectionValidator(
                c =>
                {
                    //autenticazione utente 
                    //TODO: autenticazione utente da db. Per il momento per semplicità le credenziali degli utenti sono memorizzate in config.json  

                    var currentUser = users.FirstOrDefault(u => u.UserName == c.Username);

                    if (currentUser == null)
                    {
                        c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        //log
                        return;
                    }

                    if (c.Username != currentUser.UserName)
                    {
                        c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        //log
                        return;
                    }

                    if (c.Password != currentUser.Password)
                    {
                        c.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        //log
                        return;
                    }

                    c.ReasonCode = MqttConnectReasonCode.Success;
                    //log
                });

            // e avviamo il server in modalità asincrona
            try
            {
                await _mqttServer.StartAsync(optionsBuilder.Build());           
            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }
           
        }

        public void ReceiveAsync()
        {
            _mqttServer.UseApplicationMessageReceivedHandler(e =>
            {
                String payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Console.WriteLine("--- Messaggio ricevuto ---");
                Console.WriteLine($"+ Topic = {e.ApplicationMessage.Topic}");
                Console.WriteLine($"+ Payload = {payload}");
                Console.WriteLine($"+ QoS = {e.ApplicationMessage.QualityOfServiceLevel}"); // solitamente settato a 0
                Console.WriteLine($"+ Retain = {e.ApplicationMessage.Retain}");
                Console.WriteLine();

                //log su Log4Net
                log.InfoFormat("+MESSAGE-READ: PAYLOAD: {0} -- TOPIC: {1}", payload,e.ApplicationMessage.Topic);
                
                //invoca evento
                OnMQTTMessageReceived(payload);
            });                      
        }

        public async void SendAsync(string topic, string payload)
        {
            try
            {
                var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithRetainFlag()
                .Build();

                await _mqttServer.PublishAsync(message, CancellationToken.None);

                log.InfoFormat("+MESSAGE-SEND: PAYLOAD: {0} -- TOPIC: {1}", payload, topic);
            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }            
        }
    }
}
