using Common;
using Common.Models;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
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
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(IdentifyFace)}' with message: {message}");

            var face = message.FromJson<Face>();
            var detectedFaces = await detectFace(face.Id, log);

            if (detectedFaces == null || detectedFaces.Count == 0)
            {
                log.Info($"No face was detected in {face}");
                return;
            }
            else if (detectedFaces.Count > 1)
            {
                log.Info($"Multiple faces detected on rectangle");
                // TODO: should take a face that occupies the most space
                // meantime taking the first
            }

            var cognitiveFaceId = detectedFaces[0].FaceId;
            var cognitivePersonId = await identifyFace(cognitiveFaceId, log);
            if(cognitivePersonId == null)
            {
                log.Info($"Didn't indetify person in {face}");
                // TODO: send here for tagging on slack
            }
            else
            {
                await storeFaceUserMapping(face.Id, cognitivePersonId);
                // TODO: send to slack: user identified
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

        private static async Task<CognitiveDetectResponse> detectFace(Guid atmosphereFaceId, TraceWriter log)
        {
            if (atmosphereFaceId == Guid.Empty) throw new ArgumentNullException(nameof(atmosphereFaceId));

            try
            {
                return await FaceAPIClient.Call<CognitiveDetectResponse>(
                    $"/detect",
                    new { url = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{atmosphereFaceId.ToString("D")}.jpg" },
                    log);
            }
            catch (InvalidOperationException ex)
            {
                log.Error("Failed while trying to detect (first phase) face", ex);
                return null;
            }
        }

        private static async Task<string> identifyFace(string cognitiveFaceId, TraceWriter log)
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
                        maxNumOfCandidatesReturned = 3,
                        confidenceThreshold = 0.5
                    },
                    log);

                return result
                    .FirstOrDefault()
                    ?.Candidates
                    .FirstOrDefault()
                    ?.PersonId;
            }
            catch (InvalidOperationException ex)
            {
                log.Error("Failed while trying to identify (second phase) face", ex);
                return null;
            }
        }
    }
}