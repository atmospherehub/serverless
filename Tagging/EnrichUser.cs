using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using Tagging.Models;

namespace Tagging
{
    public static class EnrichUser
    {
        private static readonly string API_ADDRESS_FORMAT = $"https://slack.com/api/users.info?token={Settings.SLACK_API_TOKEN}&user={{0}}";
        private static readonly HttpClient _client = new HttpClient();

        [FunctionName(nameof(EnrichUser))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-face-enrich-user", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Queue trigger '{nameof(EnrichUser)}' with message: {message}");
            var userMap = message.FromJson<UserMap>();

            var userInfo = await _client.GetAsync<UserInfoResponse>(String.Format(API_ADDRESS_FORMAT, userMap.SlackUid));
            log.Info($"Received from Slack API {userInfo.ToJson()}");

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    @"UPDATE [dbo].[UserMap] SET FirstName = @FirstName, LastName = @LastName, Email = @Email
                      WHERE SlackUid = @SlackUid",
                    new
                    {
                        FirstName = userInfo.User.profile.first_name,
                        LastName = userInfo.User.profile.last_name,
                        Email = userInfo.User.profile.email,
                        SlackUid = userMap.SlackUid
                    });
            }
        }
    }
}