using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Utils
{
    public class PingServer
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public PingServer(string ip, int port)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            var listener = new HttpListener();

            listener.Prefixes.Add($"http://{ip}:{port}/"); // ip e porta in ascolto del server
            // avviamo il server
            listener.Start();

            while (true)
            {

                var context = listener.GetContext();

                // avviamo un thread per rispondere
                ThreadPool.QueueUserWorkItem(o => HandleRequest(context));

            }
        }

        private void HandleRequest(object state)
        {
            try
            {
                var context = (HttpListenerContext)state;

                context.Response.StatusCode = 200;
                context.Response.SendChunked = false;

                // prepariamo la risposta
                var bytes = Encoding.UTF8.GetBytes("<HTML><BODY> Attivo</BODY></HTML>");
                context.Response.ContentLength64 = bytes.Length;

                // inviamo
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);

            }
            catch (Exception e)
            {
                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }
        }
    }



}
