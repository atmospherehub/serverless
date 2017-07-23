using Microsoft.ProjectOxford.Common.Contract;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text;
using System.Web;
using System.Data.SqlClient;
using CM = System.Configuration.ConfigurationManager;
using Dapper;

private static readonly string FACE_API_SERVICE = CM.AppSettings["FaceAPIService"];
public static readonly string connectionString = CM.ConnectionStrings["Atmosphere"].ConnectionString;
public const string CONTAINER_NAME_OUTPUT = "rectangles";
public const int MAX_FACES_PER_PERSON = 248;

public static async Task Run(string faceForRecognition, TraceWriter log)
{
    log.Info($"Triggered  - slackInput:{faceForRecognition}");
    var slackInput = JsonConvert.DeserializeObject<SlackNotification>(faceForRecognition);
    var totalCombined = await getCombinedVotes(slackInput, log);
    var totalVotesPerUser = await getTotalVotesPerUser(slackInput, log);

    if (totalCombined < 0)
    {
        log.Info($"Not enough votes for single user");
        return;
    }
    if (totalVotesPerUser > MAX_FACES_PER_PERSON)
    {
        log.Info($"Reached limit of images per person");
        return;
    }

    var user = await getUsersMap(slackInput, log);
    if (user == null)
    {

        user = new UserMap
        {
            SlackUid = slackInput.FaceUserId,
            CognitiveUid = (await callFacesAPI<PersonCreatedResponse>(
                null,
                new { name = slackInput.FaceUserId },
                log)).PersonId
        };
        await saveUsersMap(user, log);
    }

    await callFacesAPI<AddFaceResponse>(
        $"/{user.CognitiveUid}/persistedFaces",
        new
        {
            url = $"{CM.AppSettings["images_endpoint"]}/{CONTAINER_NAME_OUTPUT}/{slackInput.FaceId}.jpg"
        },
        log);
}

private static async Task<T> callFacesAPI<T>(string apiPath, dynamic requestObject, TraceWriter log)
{
    var payload = JsonConvert.SerializeObject(requestObject ?? new { });
    log.Info($"Calling API at path [{FACE_API_SERVICE}/persons{apiPath}] with payload {payload}");
    using (var client = new HttpClient())
    using (var request = new HttpRequestMessage(HttpMethod.Post, $"{FACE_API_SERVICE}/persons{apiPath}"))
    {
        request.Headers.Add("Ocp-Apim-Subscription-Key", CM.AppSettings["FaceServiceAPIKey"]);
        if (apiPath != null)
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);
        var contents = await response.Content.ReadAsStringAsync();        
        if(!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Received result from faces API {response.StatusCode}: {contents}");

        log.Info($"Received result from faces API {response.StatusCode}: {contents}");
        return JsonConvert.DeserializeObject<T>(contents);
    }
}

private static async Task saveUsersMap(UserMap data, TraceWriter log)
{
    log.Info($"Save usersMap for Cognitive person: {data.CognitiveUid}");
    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            @"INSERT INTO [dbo].[UsersMap] ([SlackUid], [CognitiveUid]) 
              VALUES (@SlackUid, @CognitiveUid)",
            data);
    }
}

private static async Task<UserMap> getUsersMap(SlackNotification slackInput, TraceWriter log)
{
    log.Info($"get usersMap {slackInput.FaceUserId}");
    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        return await connection.QuerySingleOrDefaultAsync<UserMap>(
            "SELECT * FROM [dbo].[UsersMap] WHERE SlackUid=@FaceUserId",
            slackInput);
    }
}

private static async Task<int> getCombinedVotes(SlackNotification slackInput, TraceWriter log)
{
    log.Info($"get CombinedVotes: {slackInput.FaceId} - {slackInput.FaceUserId}");
    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM [dbo].[FaceTags]
              WHERE FaceId=@FaceId AND FaceUserId=@FaceUserId",
            slackInput);
    }
}

private static async Task<int> getTotalVotesPerUser(SlackNotification slackInput, TraceWriter log)
{
    log.Info($"get TotalVotesPerUser: {slackInput.FaceUserId}");
    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
              FROM [dbo].[FaceTags]
              WHERE FaceUserId=@FaceUserId",
            slackInput);
    }
}

public class SlackNotification
{
    public string FaceId { get; set; }
    public string FaceUserId { get; set; }
    public string TaggedById { get; set; }
    public string TaggedByName { get; set; }
    public DateTimeOffset Time { get; set; }
}

public class UserMap
{
    public string SlackUid { get; set; }
    public string CognitiveUid { get; set; }
}

public class PersonCreatedResponse
{
    public string PersonId { get; set; }
}

public class AddFaceResponse
{
    public string PersistedFaceId { get; set; }
}