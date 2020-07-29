﻿using log4net;
using MQTTnet;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class ClientAMQP
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly String queue;

        public EventHandler<String> AMQPMessageReceived;
        private static readonly ILog log = LogManager.GetLogger(typeof(ClientAMQP));

        protected virtual void OnAMQPMessageReceived(String message)
        {
            if (AMQPMessageReceived != null)
                AMQPMessageReceived(this, message);
        }

        public void CreateExchange(String exchangeName, string type)
        {
            //TODO IMPLEMENTARE CON ENUMERATORE
            //l'exchange fanout inoltra i messaggi in tutte le code disponibili
            //broadcast
            switch (type)
            {
                case "fanout":
                    _channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout);
                break;
                case "direct":
                    _channel.ExchangeDeclare(exchange: "direct_message",type: "direct");
                break;
            }
        }

        public void CreateQueue(String queueName)
        {
            if (_channel != null && _channel.IsOpen)
            {
                //dichiarazione coda
                _channel.QueueDeclare(queue: queueName,
                                        durable: false,
                                        exclusive: false,
                                        autoDelete: true,
                                        arguments: null);                
            }            
        }

        public void BindQueue(String queueName, String exchangeName, String routingKey)
        {
            //associazione exhange a coda
            _channel.QueueBind(queue: queueName,
              exchange: exchangeName,
              routingKey: routingKey);
        }

        public ClientAMQP()
        {       
            Config config = Utils.ReadConfiguration();
            var factory = new ConnectionFactory();            
            factory.DispatchConsumersAsync = true;
            factory.UserName = config.Communications.AMQP.UserName;
            factory.Password = config.Communications.AMQP.Password;
            factory.HostName = config.Communications.AMQP.Ip;
            factory.Port = config.Communications.AMQP.Port;
            factory.VirtualHost = config.Communications.AMQP.VirtualHost;
            //inizializzazione connessione e canale di comunicazione
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();         
            
        }

        ~ClientAMQP()
        {
            Close();
        } 
     
        public void Close()
        {
            if (_channel.IsClosed) return;
            
            _channel.Close();
            _connection.Close();
        }

        public async Task SendMessageAsync(String exchange, String routingKey, String message)
        {
            try
            {
                await Task.Run(() =>
                {
                    //preconditions 
                    if (String.IsNullOrEmpty(message)) throw new ArgumentNullException();

                    //IBasicProperties props = _channel.CreateBasicProperties();
                    //props.Headers = new Dictionary<string, object>();
                    //props.Headers.Add("type", type);

                    //send            
                    var body = Encoding.UTF8.GetBytes(message);
                    _channel.BasicPublish(exchange: exchange,
                                         routingKey: routingKey,
                                         basicProperties: null,
                                         body: body);

                    
                    //output console 
                    Console.WriteLine(" [x] Sent {0}", message);
                    log.InfoFormat("+MESSAGE-SEND: PAYLOAD: {0}", message);
                });
            }
            
            catch (Exception e)
            {
               log.ErrorFormat("!ERROR: {0}", e.ToString());
            }
        }


        public void ReceiveMessageAsync(string queueName)
        {
            try
            {
                var consumer = new AsyncEventingBasicConsumer(_channel);
                String message = "";
                consumer.Received += async (model, ea) =>
                {
                    
                    var body = ea.Body;
                    
                    message = Encoding.UTF8.GetString(body.ToArray());                  
                    //Console.WriteLine(" [x] Msg:  {0}", message);
                    log.InfoFormat("+MESSAGE-READ: PAYLOAD: {0}", message);
                    OnAMQPMessageReceived(message);
                    await Task.Yield();
                };
                _channel.BasicConsume(queue: queueName,
                                     autoAck: true,
                                     consumer: consumer);
            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }       
        }
    }
}
