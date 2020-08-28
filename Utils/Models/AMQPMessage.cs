using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public enum AMQPMessageType
    {
        Query, QueryResult, Telemetry
    }
    public class AMQPMessage
    {
        [JsonProperty("type")]
        public AMQPMessageType Type { get; set; }
        [JsonProperty("data")]
        public String Data { get; set; }
        [JsonProperty("sender")]
        public String Sender { get; set; }
    }

    public class Query
    {
        [JsonProperty("period")]
        public int Period { get; set; }
        [JsonProperty("machineid")]
        public string MachineId { get; set; }
        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

    }



    public class QueryResult
    {
        [JsonProperty("result")]
        public bool Result { get; set; }
        [JsonProperty("time")]
        public long Time { get; set; }
        [JsonProperty("payload")]
        public String Payload { get; set; }
        [JsonProperty("machine_id")]
        public String MachineId { get; set; }
    }
}
