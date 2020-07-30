using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;
using log4net.Config;
using Utils;
using Timer = System.Threading.Timer;

namespace Monitoring
{
    public partial class FormPrincipale : Form
    {
        List<Modulo> moduli;
        private static readonly ILog log = LogManager.GetLogger(typeof(FormPrincipale));
        static readonly HttpClient client = new HttpClient();
        private static Config _config;
        public FormPrincipale()
        {
            InitializeComponent();
            _config = Utils.Utils.ReadConfiguration();
            moduli = _config.Monitoring.Modules;
            StartCheck(moduli);
        }

        List<Timer> timers = new List<Timer>();
        private void StartCheck(List<Modulo> moduli)
        {
            foreach(Modulo m in moduli)
            {
                Timer t = new Timer(Ping, m, 0, m.Timing * 1000);
                timers.Add(t);
            }
        }

        private async void Ping(object modulo)
        {
            Modulo m = (Modulo)modulo;
            string name = m.Name;
            string ip = m.Ip;
            int port = m.Port;
            try
            {
                string uri = $"http://{ip}:{port}/";
                string responseBody = await client.GetStringAsync(uri);
                if (responseBody != null)
                    switch (name)
                    {
                        #region Set colore Verde
                        case "Alerting":
                            pAlerting.BackColor = Color.Green;
                            break;
                        case "Database":
                            pDatabase.BackColor = Color.Green;
                            break;
                        case "Hub":
                            pHub.BackColor = Color.Green;
                            break;
                        case "Grafana":
                            pGrafana.BackColor = Color.Green;
                            break;
                        case "Nodered":
                            pNodered.BackColor = Color.Green;
                            break;
                        case "RabbitMQ":
                            pRabbit.BackColor = Color.Green;
                            break;
                        #endregion
                    }

                else
                    
                    switch (name)
                    {
                        #region Set colore Rosso
                        case "Alerting":
                            pAlerting.BackColor = Color.Red;
                            break;
                        case "Database":
                            pDatabase.BackColor = Color.Red;
                            break;
                        case "Hub":
                            pHub.BackColor = Color.Red;
                            break;
                        case "Grafana":
                            pGrafana.BackColor = Color.Red;
                            break;
                        case "Nodered":
                            pNodered.BackColor = Color.Red;
                            break;
                        case "RabbitMQ":
                            pRabbit.BackColor = Color.Red;
                            break;
                        #endregion
                    }

            }
            catch(Exception e)
            {
                switch (name)
                {
                    #region Set colore Rosso
                    case "Alerting":
                        pAlerting.BackColor = Color.Red;
                        break;
                    case "Database":
                        pDatabase.BackColor = Color.Red;
                        break;
                    case "Hub":
                        pHub.BackColor = Color.Red;
                        break;
                    case "Grafana":
                        pGrafana.BackColor = Color.Red;
                        break;
                    case "Nodered":
                        pNodered.BackColor = Color.Red;
                        break;
                    case "RabbitMQ":
                        pRabbit.BackColor = Color.Red;
                        break;
                    #endregion
                }

                log.ErrorFormat("!ERROR: {0}", e.ToString());
            }    
        }

        private void FormPrincipale_Load(object sender, EventArgs e)
        {

        }
    }
}
