using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Functions.Recognition.Models;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Functions.Recognition
{
    public static class ClearIdentified
    {
        [FunctionName(nameof(ClearIdentified))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-face-undo-identify", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Queue trigger '{nameof(ClearIdentified)}' with message: {message}");
            var undoMessage = message.FromJson<UndoIdentifyMessage>();

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                await connection.ExecuteScalarAsync<int>(
                    "UPDATE [dbo].[Faces] SET UserId = NULL WHERE Id = @FaceId",
                    new { FaceId = undoMessage.FaceId });
            }
        }
    }
}