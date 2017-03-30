using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Common.Contract;

public static void Run(string message, ICollector<Image> facesTable, TraceWriter log)
{
    log.Info($"Triggered StoreTable by: {message}");
    var processedImage = JsonConvert.DeserializeObject<Image>(message);
    processedImage.PartitionKey = DateTime.UtcNow.ToString("yy-MM-dd");
    processedImage.RowKey = Path.GetFileNameWithoutExtension(processedImage.ImageName);
    facesTable.Add(processedImage);
}

public class Image
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }

    public string ImageName { get; set; }
    public Emotion[] Rectangles { get; set; }
}