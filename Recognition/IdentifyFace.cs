using Common;
using Common.Models;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Recognition.Models;
using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Recognition
{
    public static class IdentifyFace
    {
        [FunctionName(nameof(IdentifyFace))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-images-in-db", "identify-face", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            [ServiceBus("atmosphere-face-not-identified", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> notIdentifiedQueue,
            [ServiceBus("atmosphere-face-identified", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> identifiedQueue,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(IdentifyFace)}' with message: {message}");

            var face = message.FromJson<Face>();

            var cognitiveFaceId = await detectFace(face.Id, log); 
            if(cognitiveFaceId == null)
            {
                log.Info($"No face was detected in {face}");
                return;
            }

            var candidate = await identifyFace(cognitiveFaceId, log);
            if(candidate == null)
            {
                log.Info($"Didn't indetify person in {face}");
                notIdentifiedQueue.Add(message);
            }
            else
            {
                await storeFaceUserMapping(face.Id, candidate.PersonId);
                identifiedQueue.Add(Tuple.Create(candidate, face).ToJson());
            }
        }
        private static async Task storeFaceUserMapping(Guid atmosphereFaceId, string cognitivePersonId)
        {
            if (atmosphereFaceId == Guid.Empty) throw new ArgumentNullException(nameof(atmosphereFaceId));
            if (String.IsNullOrEmpty(cognitivePersonId)) throw new ArgumentNullException(nameof(cognitivePersonId));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "UPDATE [dbo].[Faces] SET UserId = (SELECT UserId FROM [dbo].[UsersMap] WHERE CognitiveUid = @CognitiveUid) WHERE Id = @FaceId",
                    new { FaceId = atmosphereFaceId, CognitiveUid = cognitivePersonId });
            }
        }

        private static async Task<string> detectFace(Guid atmosphereFaceId, TraceWriter log)
        {
            if (atmosphereFaceId == Guid.Empty) throw new ArgumentNullException(nameof(atmosphereFaceId));

            try
            {
                var result = await FaceAPIClient.Call<CognitiveDetectResponse>(
                    $"/detect",
                    new { url = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{atmosphereFaceId.ToString("D")}.jpg" },
                    log);
                return result
                    .FirstOrDefault()
                    ?.FaceId;
            }
            catch (InvalidOperationException ex)
            {
                log.Error("Failed while trying to detect (first phase) face", ex);
                return null;
            }
        }

        private static async Task<CognitiveIdentifyResponse.Candidate> identifyFace(string cognitiveFaceId, TraceWriter log)
        {
            if (String.IsNullOrEmpty(cognitiveFaceId)) throw new ArgumentNullException(nameof(cognitiveFaceId));

            try
            {
                var result = await FaceAPIClient.Call<CognitiveIdentifyResponse>(
                    $"/identify",
                    new
                    {
                        personGroupId = Settings.FACE_API_GROUP_NANE,
                        faceIds = new string[] { cognitiveFaceId },
                        maxNumOfCandidatesReturned = 1,
                        confidenceThreshold = 0.80
                    },
                    log);

                return result
                    .FirstOrDefault()
                    ?.Candidates
                    .FirstOrDefault();
            }
            catch (InvalidOperationException ex)
            {
                log.Error("Failed while trying to identify (second phase) face", ex);
                return null;
            }
        }
    }
}
