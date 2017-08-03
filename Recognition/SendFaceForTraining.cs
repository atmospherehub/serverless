using Common;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Recognition.Models;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Recognition
{
    public static class SendFaceForTraining
    {
        private static readonly int REQUIRED_VOTES_FROM_DIFF_USERS = Int32.Parse(Settings.Get("REQUIRED_VOTES_FOR_SENDING_TO_FACE_API") ?? "2");
        private const int MAX_IMAGES_PER_PERSON = 248;

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
            var votesForSamePerson = await getFaceTagsOnImageCount(slackInput.UserId, slackInput.FaceId);
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

            if (await getFaceTagsCount(slackInput.UserId) > MAX_IMAGES_PER_PERSON)
            {
                log.Info($"Reached limit of images per person. Maximum amount is {MAX_IMAGES_PER_PERSON}. This means no more training required for the person.");
                return;
            }

            // ensure mapping of our identifier of user - SlackId has a mapping to user identifier on Cognetive service
            var user = await getUsersMap(slackInput.UserId);
            if (user == null)
            {
                log.Info($"Creating mapping for slackId and cognitiveId");
                var faceApiMapping = await FaceAPIClient.Call<CreateUserResponse>(
                    $"/persongroups/{Settings.FACE_API_GROUP_NANE}/persons", 
                    new { name = slackInput.UserId }, 
                    log);
                user = new UserMap
                {
                    UserId = slackInput.UserId,
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
                await FaceAPIClient.Call<dynamic>(
                    $"/persongroups/{Settings.FACE_API_GROUP_NANE}/persons/{user.CognitiveUid}/persistedFaces",
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

        private static async Task<UserMap> getUsersMap(string userId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.QuerySingleOrDefaultAsync<UserMap>(
                    "SELECT * FROM [dbo].[UsersMap] WHERE UserId = @UserId",
                    new { UserId = userId });
            }
        }

        private static async Task saveUsersMap(UserMap userMap)
        {
            if (userMap == null) throw new ArgumentNullException(nameof(userMap));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(
                    "INSERT INTO [dbo].[UsersMap] ([UserId], [CognitiveUid]) VALUES (@UserId, @CognitiveUid)",
                    userMap);
            }
        }

        private static async Task<int> getFaceTagsOnImageCount(string userId, string faceId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (String.IsNullOrEmpty(faceId)) throw new ArgumentNullException(nameof(faceId));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM [dbo].[FaceTags] WHERE FaceId = @FaceId AND UserId = @UserId",
                    new { UserId = userId, FaceId = faceId });
            }
        }

        private static async Task<int> getFaceTagsCount(string userId)
        {
            if (String.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));

            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM [dbo].[FaceTags] WHERE UserId = @UserId",
                    new { UserId = userId });
            }
        }
    }
}