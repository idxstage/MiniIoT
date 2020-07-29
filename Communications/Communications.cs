using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Mail;
using System.Text;
using Newtonsoft.Json;
using SlackBotMessages;
using SlackBotMessages.Models;
using log4net;
using log4net.Config;

namespace Alerting
{
	class Communications
	{
		SmtpClient client = new SmtpClient();
		static SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

		public static void SendMessageSMTP(string username, string password, string from, string body, string severity, string address)
		{
			MailMessage mail = new MailMessage();


			mail.From = new MailAddress("burtinimarco1@gmail.com");
			mail.To.Add(address);
			mail.Subject = $"ADVISE FROM BOT - Severity: {severity}";
			mail.Body = body;

			SmtpServer.Port = 587;
			SmtpServer.Credentials = new System.Net.NetworkCredential(username, password);
			SmtpServer.EnableSsl = true;

			SmtpServer.Send(mail);

			
		}



		public static void SendMessageSlack(string username, string text, string token1, string token2, string token3)
		{
			var WebHookUrl = $"https://hooks.slack.com/services/{token1}/{token2}/{token3}";

			var client = new SbmClient(WebHookUrl);

			var message = new Message("New trial")
				.SetUserWithEmoji("Website", Emoji.Loudspeaker);
			message.AddAttachment(new SlackBotMessages.Models.Attachment()
				.AddField("Name", username, true)
				.SetColor("#f96332")
			);

			client.Send(message);
		}
	}

}