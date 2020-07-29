using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using SlackBotMessages;
using SlackBotMessages.Models;
using EASendMail;
using System.Net.Mail;
using log4net;
using log4net.Config;
using System.Collections;
using Alerting.Model;
using Action = Alerting.Model.Action;
using Utils;

namespace Alerting
{
    class Communications
    {
		private static readonly ILog log = LogManager.GetLogger(typeof(Communications));
		public static void SendMessageSMTP(Action a, Rule r, Dictionary<string,string> campiTele)
		{
			try
			{
				Config c = Utils.Utils.ReadConfiguration();

				MailMessage mail = new MailMessage();

				SmtpMail oMail = new SmtpMail("TryIt");

				oMail.From = c.Communications.Email.User.UserName;

				oMail.To = a.address;

				oMail.Subject = $"Sistema MiniIoT rilevata anomalia su frigo {r.Id} con livello criticità: {r.Severity}";

				oMail.TextBody = "";
				// alleghiamo tutti i campi della telemetria
				IDictionaryEnumerator k = campiTele.GetEnumerator();

				while(k.MoveNext())
				{
					oMail.TextBody += $"\n{k.Key}:{k.Value}";
				}

				// alleghiamo la regola
				oMail.TextBody += "\n\nRegola:\n";
				oMail.TextBody += $"Id: {r.Id}";
				oMail.TextBody += $"\nDescription: {r.Description}";
				oMail.TextBody += $"\nConditionOperator: {r.ConditionOperator}";
				oMail.TextBody += $"\nField: {r.Field}";
				oMail.TextBody += $"\nFrequency: {r.Frequency}";
				oMail.TextBody += $"\n\nPeriod: {r.Period}";
				oMail.TextBody += $"\nValue: {r.Value}";
				foreach (string s in r.Machine)
					oMail.TextBody += $"\nMachine: {s}";
				foreach (Action ac in r.actions)
				{
					oMail.TextBody += $"\nAction";
					oMail.TextBody += $"\nType: {ac.type}";
					oMail.TextBody += $"\nAddress: {ac.address}";
					oMail.TextBody += $"\nBody: {ac.body}";
				}

				string value;
				campiTele.TryGetValue("Value", out value);
				// alleghiamo il motivo dell'email
				oMail.TextBody += "\n\nValore out poichè:\n " + r.Field + r.ConditionOperator + r.Value;

				// alleghiamo le operazioni da fare
				oMail.TextBody += "\n\nOperation to do\n" + a.body;
				

				SmtpServer oServer = new SmtpServer(c.Communications.Email.SMTPServer);

				oServer.User = c.Communications.Email.User.UserName;
				oServer.Password = c.Communications.Email.User.Password;

				// Set 465 port
				oServer.Port = c.Communications.Email.Port;

				// si arrangia col protocollo
				oServer.ConnectType = SmtpConnectType.ConnectSSLAuto;

				EASendMail.SmtpClient oSmtp = new EASendMail.SmtpClient();
				oSmtp.SendMail(oServer, oMail);

				log.InfoFormat("+MESSAGE-SEND: From: {0} -- To: {1} -- Severity: {2}", oMail.From, a.address, a.body);
			}

			catch(Exception e)
            {
				log.ErrorFormat("!ERROR: {0} - Not send message to Gmail", e.ToString());
			}
			
		}

		public static void SendMessageSlack(string text, string address)
        {
			// canale miniIoT che fa appoggio alle api di slack: https://api.slack.com/apps

			string channel = address.Split("|")[0];
			string urlWithAccessToken = address.Split("|")[1];

			Config c = Utils.Utils.ReadConfiguration();

			try
			{
				SlackClient client = new SlackClient(urlWithAccessToken);

				client.PostMessage(username: c.Communications.Slack.UserName,
						   text: text,
						   channel: channel);

				log.InfoFormat("+MESSAGE-SEND: From: {0} -- Text: {1}", c.Communications.Slack.UserName, text);
			}
			catch(Exception e)
            {
				log.ErrorFormat("!ERROR: {0} - Not send message to Slack", e.ToString());
			}
			
		}
	}

}