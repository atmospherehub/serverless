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



private const string FACE_API_SERVICE = "https://westeurope.api.cognitive.microsoft.com/face/v1.0/persongroups/dm-atmosphere";

public static readonly string connectionString = CM.ConnectionStrings["Atmosphere"].ConnectionString;

public const string CONTAINER_NAME_OUTPUT = "rectangles";

public const int MAX_FACES_PER_PERSON = 248;

public static async Task Run(string faceForRecognition, TraceWriter log)
{
    log.Info($"Triggered  - slackInput:{faceForRecognition}");
    var slackInput = JsonConvert.DeserializeObject<SlackNotification>(faceForRecognition);
    var imgUrl = $"{CM.AppSettings["images_endpoint"]}/{CONTAINER_NAME_OUTPUT}/{slackInput.FaceId}.jpg";

    var totalCombined = await getCombinedVotes(slackInput, log);
    var totalVotesPerUser = await getTotalVotesPerUser(slackInput, log);

    if(totalCombined > 1 && totalVotesPerUser < MAX_FACES_PER_PERSON)
    {  
        var getedUsersMap = await getUsersMap(slackInput, log);
        if(getedUsersMap!= null){
            // Add face to person (face API) in Cognitive
            log.Info($"Adding face to person.");
            await addPersonFace(getedUsersMap, imgUrl, log);

            // var getedPerson = await getPerson(getedUsersMap.CognitiveUid,log);
            // log.Info($":::::::---getPerson---::::::::{getedPerson}");
        }else{
            // Create person in Cognitive
            var createdPerson = await createPerson(slackInput.FaceUserId,log);

            // Create usersMap in DB
            var userMapping = new userMap();
            userMapping.SlackUid = slackInput.FaceUserId;
            userMapping.CognitiveUid = createdPerson;
            await saveUsersMap(userMapping,log);

            // Add face to person (face API) in Cognitive
            await addPersonFace(userMapping, imgUrl, log);
        }
    }
}

private static async Task<string> createPerson(string slackUid, TraceWriter log)
{
    log.Info($"Create person in Cognitive with slackUid {slackUid}");

    using (var client = new HttpClient())
    using (var request = new HttpRequestMessage(HttpMethod.Post, FACE_API_SERVICE + "/persons"))
    {
        dynamic jsonObject = new JObject();
        jsonObject.name = slackUid;
        jsonObject.userData = slackUid;

        request.Headers.Add("Ocp-Apim-Subscription-Key", CM.AppSettings["FaceServiceAPIKey"]);
        byte[] byteData = Encoding.UTF8.GetBytes(jsonObject.ToString());
        request.Content = new ByteArrayContent(byteData);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await client.SendAsync(request);
        var contents = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<personCreatedResponse>(contents).personId;
    }
}

private static async Task<string> addPersonFace(userMap data, string image, TraceWriter log)
{
    log.Info($"Add person face {data.CognitiveUid}, {image}");
    using (var client = new HttpClient())
    using (var request = new HttpRequestMessage(HttpMethod.Post, FACE_API_SERVICE+"/persons/"+data.CognitiveUid + "/persistedFaces"))
    {
        dynamic jsonObject = new JObject();
        jsonObject.url = image;

        request.Headers.Add("Ocp-Apim-Subscription-Key", CM.AppSettings["FaceServiceAPIKey"]);
        byte[] byteData = Encoding.UTF8.GetBytes(jsonObject.ToString());
        request.Content = new ByteArrayContent(byteData);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await client.SendAsync(request);
        var contents = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<personCreatedResponse>(contents).personId;
    }
}

private static async Task<string> getPerson(string CognitiveUid, TraceWriter log)
{
    log.Info($"Create person in Cognitive with SlackId {CognitiveUid}");

    using (var client = new HttpClient())
    using (var request = new HttpRequestMessage(HttpMethod.Get, FACE_API_SERVICE+"/persons/" + CognitiveUid))
    {
        request.Headers.Add("Ocp-Apim-Subscription-Key", CM.AppSettings["FaceServiceAPIKey"]);
        var response = await client.SendAsync(request);
        var contents = await response.Content.ReadAsStringAsync();
        return contents;
    }
}

private static async Task<string> saveUsersMap(userMap data, TraceWriter log)
{
    log.Info($"save usersMap {data.CognitiveUid}");
    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        await connection.ExecuteAsync(
            @"
             INSERT INTO [dbo].[UsersMap] (
                    [SlackUid],
                    [CognitiveUid]) VALUES (
                    @SlackUid,
                    @CognitiveUid)",
            data);
        return "---";
    }   
}

private static async Task<userMap> getUsersMap(SlackNotification slackInput, TraceWriter log)
{
    log.Info($"get usersMap {slackInput.FaceUserId}");
    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        var res = await connection.QuerySingleOrDefaultAsync<userMap>(
            @"SELECT * FROM [dbo].[UsersMap] WHERE SlackUid=@FaceUserId",
            slackInput);
        return res;
      }   
}

private static async Task<int> getCombinedVotes(SlackNotification slackInput, TraceWriter log)
{
    log.Info($"get CombinedVotes: {slackInput.FaceId} - {slackInput.FaceUserId}");
    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(
            $@"SELECT COUNT(*)
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
            $@"SELECT COUNT(*)
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

public class userMap
{
    public string SlackUid { get; set; }
    public string CognitiveUid { get; set; }
}

public class personCreatedResponse{
    public string personId { get; set; }
}

public class personGetResponse{
    public string personId { get; set; }
    public string[] persistedFaceIds { get; set; }
    public string name { get; set; }
    public string userDate { get; set; }
}