using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Reflection.Metadata;

namespace Utils
{
    public class Config
    {   
        public List<User> Users { get; set; } = new List<User>();

        public InfluxDB InfluxDB { get; set; }

        public Alerting Alerting { get; set; }

        public Hub Hub { get; set; }

        public Communications Communications { get; set; }

        public Monitoring Monitoring { get; set; }

        public MongoDB MongoDB { get; set; }
    }
    public class InfluxDB
    {
        public int Port { get; set; }
        public String Ip { get; set; }
        public string RetentionPolicy { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

    }

    public class MongoDB
    {
        public String ConnectionString { get; set; }
    }
    public class Alerting
    {
        
    }
    public class Hub
    {
        
    }
    public class Communications
    {
        public AMQP AMQP { get; set; }
        public MQTT MQTT { get; set; }
        public Email Email { get; set; }
        public Slack Slack { get; set; }
    }
    public class AMQP
    {
        //parametri per la connessione al broker RabbitMQ
        public String Ip { get; set; }
        public int Port { get; set; }       
        public String VirtualHost { get; set; }

        //credenziali per l'accesso al broker AMQP RabbitMQ
        public String UserName { get; set; }
        public String Password { get; set; }
        public string Queue { get; set; }
        public string Exchange { get; set; }
    }

    public class MQTT
    {       
        public int Port { get; set; }       
        public List<User> Users { get; set; }

    }
    public class User
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class Email
    {
        public String SMTPServer { get; set; }
        public User User { get; set; }
        public int Port { get; set; }
    }
    public class Slack
    {
        public String UserName { get; set; }
        public String Channel { get; set; }
    }

    public class Monitoring
    {
        public List<Modulo> Modules { get; set; }
    }

    public class Modulo
    {
        public string Name { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public int Timing { get; set; }
    }

}