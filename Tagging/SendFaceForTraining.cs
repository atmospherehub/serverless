using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
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
        private static readonly int REQUIRED_VOTES_FROM_DIFF_USERS = Int32.Parse(Settings.Get("REQUIRED_VOTES_FOR_SENDING_TO_FACE_API") ?? "2");
        private const int MAX_IMAGES_PER_PERSON = 248;

        private static readonly HttpClient _client = new HttpClient();

        [FunctionName(nameof(SendFaceForTraining))]
        public static async Task Run(
            [ServiceBusTrigger("atmosphere-face-tagged", AccessRights.Listen, Connection = Settings.SB_CONN_NAME)]string message,
            [ServiceBus("atmosphere-face-cleanup", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> cleanupQueue,
            [ServiceBus("atmosphere-face-training-sent", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> trainingSentQueue,
            [ServiceBus("atmosphere-face-enrich-user", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> enrichQueue,
            TraceWriter log)
        {
            log.Info($"Queue trigger '{nameof(SendFaceForTraining)}' with message: {message}");
            var slackInput = message.FromJson<TaggingMessage>();

            // check if the slack message's votes got to the point that
            // * The required number of people taged face as same person => the image should be submitted to congetive service 
            // * Not enough votes => wait for more
            // * Too many votes - edge case, only if two users voted simultaniously 
            var votesForSamePerson = await getFaceTagsOnImageCount(slackInput.FaceUserId, slackInput.FaceId);
            log.Info($"Votes for same person was {votesForSamePerson} while required is {REQUIRED_VOTES_FROM_DIFF_USERS}");
            if (votesForSamePerson < REQUIRED_VOTES_FROM_DIFF_USERS)
            {
                log.Info($"Not enough votes for face on image. Requires at least {REQUIRED_VOTES_FROM_DIFF_USERS} different user votes to vote for the same user.");
                return;
            }
            else if (votesForSamePerson > REQUIRED_VOTES_FROM_DIFF_USERS)
            {
                log.Info($"Voting alredy closed for the faceId {slackInput.FaceId}, meaning the image was already submitted.");
                return;
            }

            if (await getFaceTagsCount(slackInput.FaceUserId) > MAX_IMAGES_PER_PERSON)
            {
                log.Info($"Reached limit of images per person. Maximum amount is {MAX_IMAGES_PER_PERSON}. This means no more training required for the person.");
                return;
            }

            // ensure mapping of our identifier of user - SlackId has a mapping to user identifier on Cognetive service
            var user = await getUsersMap(slackInput.FaceUserId);
            if (user == null)
            {
                log.Info($"Creating mapping for slackId and cognitiveId");
                var faceApiMapping = await callFacesAPI<CreateUserResponse>(null, new { name = slackInput.FaceUserId }, log);
                user = new UserMap
                {
                    SlackUid = slackInput.FaceUserId,
                    CognitiveUid = faceApiMapping.PersonId
                };
                await saveUsersMap(user);
                enrichQueue.Add(user.ToJson());
                log.Info($"Stored new user map {user.ToJson()}");
            }

            try
            {
                // image submitted only once to congetive when a required number of votes reached
                // once submitted we need to prevent any more voting on slack image
                await callFacesAPI<dynamic>(
                    $"/{user.CognitiveUid}/persistedFaces",
                    new { url = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_RECTANGLES}/{slackInput.FaceId}.jpg" },
                    log);
                trainingSentQueue.Add(message);
            }
            catch (InvalidOperationException)
            {
                // something went wrong, probably with image itself (e.g. blured) close voting
                // and remove all taggin info perfored till now
                log.Info($"Going to cleanup tagging for this faceId");
                cleanupQueue.Add(message);
            }
        }

        private static async Task<T> callFacesAPI<T>(string apiPath, dynamic requestObject, TraceWriter log)
            where T : class
        {
            var payload = ((object)(requestObject ?? new { })).ToJson();
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
                return contents.FromJson<T>();
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