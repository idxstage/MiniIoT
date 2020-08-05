using log4net;
using log4net.Repository;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Utils
{
    public class Utils
    {
        /// <summary>
        /// Legge file di configurazione config.json e ritorna istanza del modello Config
        /// </summary>
        /// <param name="currentPath"></param>
        /// <returns>Config</returns>
        public static Config ReadConfiguration()
        {            
            //path file di configurazione
            var currentPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var filePath = $"{currentPath}\\config.json";            
            Config config = null;
            if (File.Exists(filePath))
            {
                using var r = new StreamReader(filePath);
                var json = r.ReadToEnd();
                config = JsonConvert.DeserializeObject<Config>(json);    
                
            }

            return config;
        }      
    }
}
