using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using Functions.Recognition.Models;
using Common.Models;

namespace Functions.Recognition
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

            var userInfo = await _client.GetAsync<SlackUserInfoResponse>(String.Format(API_ADDRESS_FORMAT, userMap.UserId));
            log.Info($"Received from Slack API {userInfo.ToJson()}");

            if (!userInfo.Success)
                return;

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    @"UPDATE [dbo].[UsersMap] SET FirstName = @FirstName, LastName = @LastName, Email = @Email
                      WHERE UserId = @UserId",
                    new
                    {
                        FirstName = userInfo.User.profile.first_name,
                        LastName = userInfo.User.profile.last_name,
                        Email = userInfo.User.profile.email,
                        UserId = userMap.UserId
                    });
            }
        }
    }
}