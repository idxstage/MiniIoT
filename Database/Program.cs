using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;


namespace Database
{
    public enum MessageType
    {
        Telemetria, Messaggio
    }
    public class Program
    {
        private static DBConnection _dbconnection;
        private static ClientAMQP _amqpconn;
        private static Config _config;
        static void Main(string[] args)
        {            

            _dbconnection = new DBConnection();
            _amqpconn = new ClientAMQP();
            _config = Utils.Utils.ReadConfiguration();

            Modulo modulo = _config.Monitoring.Modules.Find(x => x.Name.Contains("Database"));
            AliveServer(modulo.Ip, modulo.Port);

            var exchange = _config.Communications.AMQP.Exchange;
            var queue = _config.Communications.AMQP.Queue;

            _amqpconn.CreateExchange(exchange, "direct");

            //creo coda database
            _amqpconn.CreateQueue(queue);

            //bind coda database a exchange ed routing key 'common'
            //canale generale per la ricezione delle telemetrie da parte dell'hub
            _amqpconn.BindQueue(queue , exchange, "common");

            //bind coda database a exchange e routing key 'database' 
            //riservato per le comunicazioni al database (es. richieste telemetrie da parte del modulo Alerting)
            _amqpconn.BindQueue(queue, exchange, "database");
                              
            //imposto evento ricezione messaggi AMQP
            _amqpconn.AMQPMessageReceived += OnAMQPMessageReceived;
            _amqpconn.ReceiveMessageAsync(queue);

            Console.ReadLine();
        }

        public async static void OnAMQPMessageReceived(object sender, String msg)
        {
            var message = JsonConvert.DeserializeObject<AMQPMessage>(msg);
            switch (message.Type)
            {
                case AMQPMessageType.Telemetry:
                    await _dbconnection.WriteData(message.Data);
                    break;                    
                case AMQPMessageType.Query:
                    var query = JsonConvert.DeserializeObject<Query>(message.Data);
                    int period = query.Period;
                    var machineId = query.MachineId;
                    var req_sender = message.Sender;
                    var field = query.Field;
                    var result = await _dbconnection.ReadData(machineId,field, period);
                    var body = new AMQPMessage { Type = AMQPMessageType.QueryResult, Data = result, Sender = _config.Communications.AMQP.Queue };
                    var json = JsonConvert.SerializeObject(body);
                    await _amqpconn.SendMessageAsync(_config.Communications.AMQP.Exchange, req_sender, json);
                    break;
            }
          
            Console.WriteLine("{0} - {1}: ", message.Type.ToString(), message.Data);            
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
