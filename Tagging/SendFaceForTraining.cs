using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tagging.Models;

namespace Tagging
{
    public static class SendFaceForTraining
    {
        private const int REQUIRED_VOTES_FROM_DIFF_USERS = 2;
        private const int MAX_IMAGES_PER_PERSON = 248;

        private static readonly HttpClient _client = new HttpClient();

        [FunctionName(nameof(SendFaceForTraining))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-face-tagged", "face-training", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            TraceWriter log)
        {
            log.Info($"Topic trigger '{nameof(SendFaceForTraining)}' with message: {message}");

            var slackInput = message.FromJson<TaggingMessage>();            

            if (await getFaceTagsOnImageCount(slackInput.FaceUserId, slackInput.FaceId) < REQUIRED_VOTES_FROM_DIFF_USERS)
            {
                log.Info($"Not enough votes for face on image. Requires at least {REQUIRED_VOTES_FROM_DIFF_USERS} different user votes.");
                return;
            }

            if (await getFaceTagsCount(slackInput.FaceUserId) > MAX_IMAGES_PER_PERSON)
            {
                log.Info($"Reached limit of images per person. Maximum amount is {MAX_IMAGES_PER_PERSON}");
                return;
            }

            var user = await getUsersMap(slackInput.FaceUserId);
            if (user == null)
            {
                user = new UserMap
                {
                    SlackUid = slackInput.FaceUserId,
                    CognitiveUid = (await callFacesAPI(
                        null,
                        new { name = slackInput.FaceUserId },
                        log)).PersonId
                };
                await saveUsersMap(user);
                log.Info($"Stored new user map {user.ToJson()}");
            }

            await callFacesAPI(
                $"/{user.CognitiveUid}/persistedFaces",
                new { url = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{slackInput.FaceId}.jpg" },
                log);
        }

        private static async Task<dynamic> callFacesAPI(string apiPath, dynamic requestObject, TraceWriter log)
        {
            var payload = (requestObject ?? new { }).ToJson();
            log.Info($"Calling API at path [/persons{apiPath}] with payload {payload}");

            using (var request = new HttpRequestMessage(HttpMethod.Post, $"{Settings.FACE_API_URL}/persons{apiPath}"))
            {
                request.Headers.Add("Ocp-Apim-Subscription-Key", Settings.FACE_API_TOKEN);
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _client.SendAsync(request);
                var contents = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException($"Received result from faces API {response.StatusCode}: {contents}");

                log.Info($"Received result from faces API {response.StatusCode}: {contents}");
                return contents.FromJson<dynamic>();
            }
        }

        private static async Task<UserMap> getUsersMap(string faceUserId)
        {
            if (String.IsNullOrEmpty(faceUserId)) throw new ArgumentNullException(nameof(faceUserId));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.QuerySingleOrDefaultAsync<UserMap>(
                    "SELECT * FROM [dbo].[UsersMap] WHERE SlackUid = @FaceUserId",
                    new { FaceUserId = faceUserId });
            }
        }

        private static async Task saveUsersMap(UserMap userMap)
        {
            if (userMap == null) throw new ArgumentNullException(nameof(userMap));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "INSERT INTO [dbo].[UsersMap] ([SlackUid], [CognitiveUid]) VALUES (@SlackUid, @CognitiveUid)",
                    userMap);
            }
        }

        private static async Task<int> getFaceTagsOnImageCount(string faceUserId, string faceId)
        {
            if (String.IsNullOrEmpty(faceUserId)) throw new ArgumentNullException(nameof(faceUserId));
            if (String.IsNullOrEmpty(faceId)) throw new ArgumentNullException(nameof(faceId));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM [dbo].[FaceTags] WHERE FaceId = @FaceId AND FaceUserId = @FaceUserId",
                    new { FaceUserId = faceUserId, FaceId = faceId });
            }
        }

        private static async Task<int> getFaceTagsCount(string faceUserId)
        {
            if (String.IsNullOrEmpty(faceUserId)) throw new ArgumentNullException(nameof(faceUserId));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM [dbo].[FaceTags] WHERE FaceUserId = @FaceUserId",
                    new { FaceUserId = faceUserId });
            }
        }
    }
}