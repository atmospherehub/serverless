using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;

public static HttpResponseMessage Run(HttpRequestMessage req, TraceWriter log)
{
    try
    {
        var filePath = getFilePath(req, log);

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var stream = new FileStream(filePath, FileMode.Open);
        response.Content = new StreamContent(stream);
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        return response;
    }
    catch(Exception ex)
    {
        log.Error($"Failed to serve a file", ex);
        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}

private static string getScriptPath()
    => Path.Combine(getEnvironmentVariable("HOME"), @"site\wwwroot");

private static string getEnvironmentVariable(string name)
    => System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);

private static string getFilePath(HttpRequestMessage req, TraceWriter log)
{
    var pathValue = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "file", true) == 0)
        .Value;

    var path = pathValue ?? "";

    var staticFilesPath = Path.GetFullPath(getScriptPath());
    var fullPath = Path.GetFullPath(Path.Combine(staticFilesPath, path));

    if (!validateDirectory(staticFilesPath, fullPath))
        throw new ArgumentException("Invalid path");

    if (Directory.Exists(fullPath))
        throw new ArgumentException("Invalid path");

    return fullPath;
}

private static bool validateDirectory(string parentPath, string childPath)
{
    var parent = new DirectoryInfo(parentPath);
    var child = new DirectoryInfo(childPath);

    var dir = child;
    do
    {
        if (dir.FullName == parent.FullName)
        {
            return true;
        }
        dir = dir.Parent;
    } while (dir != null);

    return false;
}