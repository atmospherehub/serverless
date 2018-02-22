using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Functions.Reports.Models;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Functions.Reports
{
    public static class SendEmailReport
    {
        private const string TEMPLATE_ID = "6848c399-1e1a-4ca5-86b0-b0a97b2bff6b";
        private const string EMAIL_FROM_NAME = "Atmosphere";

        [FunctionName(nameof(SendEmailReport))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-reports", "mail-send", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(SendEmailReport)}' with message: {message}");

            var report = message.FromJson<Report>();

            if (report.Total < 10)
            {
                log.Info($"Skip sending email, since the total was: {report.Total}");
                return;
            }

            var substitutions = report
                .Results
                .SelectMany(r => new Dictionary<string, string> {
                        { $"%{r.Name}Score%", r.Score.ToString("0.00") },
                        { $"%{r.Name}Time%", TimeZoneInfo.ConvertTime(r.Time, Settings.LOCAL_TIMEZONE).DateTime.ToShortTimeString() },
                        { $"%{r.Name}Image%", $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_ZOOMIN}/{r.Id.ToString("D")}.jpg" }
                })
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            substitutions.Add("%Date%", report.StartDate.ToString("MMMM dd"));
            substitutions.Add("%Total%", report.Total.ToString());

            await sendEmail(substitutions, log);
        }

        private static async Task sendEmail(Dictionary<string, string> substitutions, TraceWriter log)
        {
            var client = new SendGridClient(Settings.SENDGRID_API_TOKEN);
            var subscribers = await getSubscribers();
            log.Info($"Found subscribers: {String.Join(", ", subscribers.Select(s => s.Email))}");

            var message = new SendGridMessage()
            {
                From = new EmailAddress(Settings.SENDER_EMAIL_ADDRESS, EMAIL_FROM_NAME),
                TemplateId = TEMPLATE_ID,
                Personalizations = subscribers
                        .Select(e => new Personalization
                        {
                            Tos = new List<EmailAddress> { e },
                            Substitutions = substitutions
                        })
                        .ToList()
            };

            var response = await client.SendEmailAsync(message);
            var responseBody = await response.Body.ReadAsStringAsync();
            log.Info($"Sent with result {response.StatusCode}: [{responseBody}]");
        }

        private static async Task<List<EmailAddress>> getSubscribers()
        {
            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return (await connection
                    .QueryAsync<EmailAddress>(
                    @"SELECT 
                        [Email],
                        [Name]
                      FROM [dbo].[ReportsSubscribers]
                      WHERE [IsDisabled] = 0"))
                    .ToList();
            }
        }
    }
}