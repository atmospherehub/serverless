using System;
using SendGrid;
using SendGrid.Helpers.Mail;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Dapper;
using CM = System.Configuration.ConfigurationManager;   

private static readonly string connectionString = CM.ConnectionStrings["Atmosphere"].ConnectionString;
private static readonly TimeZoneInfo ilTimezone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");

public static async Task Run(string message, TraceWriter log)
{
    log.Info($"Triggered SendEmailReport by: {message}");
    
    var report = JsonConvert.DeserializeObject<Report>(message);

    if(report.Total < 10)
    {
        log.Info($"Skip sending email, since the total was: {report.Total}");
        return;
    }

    var substitutions = report
        .Results
        .SelectMany(r => new Dictionary<string, string> {
            { $"%{r.Name}Score%", r.Score.ToString("0.00") },
            { $"%{r.Name}Time%", TimeZoneInfo.ConvertTime(r.Time, ilTimezone).DateTime.ToShortTimeString() },
            { $"%{r.Name}Image%", $"{CM.AppSettings["images_endpoint"]}/rectangles/{r.Id.ToString("D")}.jpg" }
        })
        .ToDictionary(kv => kv.Key, kv => kv.Value);
    substitutions.Add("%Date%", report.StartDate.ToString("MMMM dd"));
    substitutions.Add("%Total%", report.Total.ToString());
        
    await sendEmail(substitutions, log);
}

private static async Task sendEmail(Dictionary<string, string> substitutions, TraceWriter log)
{    
    var client = new SendGridClient(CM.AppSettings["sendgreed_apikey"]);
    var subscribers = await getSubscribers();
    log.Info($"Found subscribers: {String.Join(", ", subscribers.Select(s => s.Email))}");
    
    var message = new SendGridMessage()
    {
        From = new EmailAddress(CM.AppSettings["reports_sender_address"], "Atmosphere"),
        TemplateId = "6848c399-1e1a-4ca5-86b0-b0a97b2bff6b",
        Personalizations = subscribers
                .Select(e => new Personalization
                {
                    Tos = new List<EmailAddress> { e },
                    Substitutions = substitutions
                })
                .ToList()
    };   

    var response = await client.SendEmailAsync(message);
    var responseBody = await response.Body.ReadAsStringAsync();
    if(String.IsNullOrEmpty(responseBody))
        log.Info($"Sent. Result {response.StatusCode}. ");
    else
        log.Info($"Sent. Result {response.StatusCode}: {responseBody}");
}

private static async Task<List<EmailAddress>> getSubscribers()
{
    // debug: return new List<EmailAddress> { new EmailAddress { Email = "jenyay@gmail.com" }};
    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        return (await connection
            .QueryAsync<EmailAddress>(@"SELECT 
                    [Email],
                    [Name]
                FROM [dbo].[ReportsSubscribers]
                WHERE [IsDisabled] = 0"))
            .ToList();
    }
}

public class ScoreResult
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public float Score { get; set; }
    public DateTimeOffset Time { get; set; }
    public string Image { get; set; }    
}

public class Report
{
    public DateTime StartDate { get; set; }     
    public DateTime EndDate { get; set; }      
    public string ReportName { get; set; }        
    public int Total { get; set; }   
    public ScoreResult[] Results { get; set; }
}