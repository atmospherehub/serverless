using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Azure; 
using Microsoft.WindowsAzure.Storage; 
using Microsoft.WindowsAzure.Storage.Blob;
using CM = System.Configuration.ConfigurationManager;

public const string CONTAINER_NAME = "faces";

public static void Run(string message, TraceWriter log, out string notifyMessage)
{
    log.Info($"Triggered PostProcessImage by: {message}");
    var processedImage = JsonConvert.DeserializeObject<ProcessedImage>(message);

    if(processedImage.Rectangles == null || processedImage.Rectangles.Count == 0)
    {
        log.Info($"No faces were detected => deleting blob");
        notifyMessage = null;
        deleteBlob(processedImage.ImageName);
    }
    else
    {
        log.Info($"{processedImage.Rectangles.Count} faces detected => rasing notifications");
        notifyMessage = message;
    }    
}

private static void deleteBlob(string fileName)
{
    var storageAccount = CloudStorageAccount.Parse(CM.AppSettings["funcatmosphere_STORAGE"]);
    var blobClient = storageAccount.CreateCloudBlobClient();
    var container = blobClient.GetContainerReference(CONTAINER_NAME);
    var blockBlob = container.GetBlockBlobReference(fileName);
    blockBlob.DeleteIfExists();
}

public class ProcessedImage
{
    public string ImageName { get; set; }

    public JArray Rectangles { get; set; }
}