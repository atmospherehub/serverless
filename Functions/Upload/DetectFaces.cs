using Common;
using Functions.Upload.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Functions.Upload
{
    public static class DetectFaces
    {
        [FunctionName(nameof(DetectFaces))]
        public static async Task Run(
            [BlobTrigger("faces/{name}", Connection = Settings.STORAGE_CONN_NAME)]CloudBlockBlob inputImage,
            [ServiceBus("atmosphere-processed-images", AccessRights.Send, Connection = Settings.SB_CONN_NAME, EntityType = EntityType.Queue)] ICollector<string> outputQueue,
            TraceWriter log)
        {
            log.Info($"Blob trigger '{nameof(DetectFaces)}' with {inputImage.Name}");

            var result = await FaceAPIClient.Call<IdentifyResponse>(
                $"/detect?returnFaceAttributes=emotion",
                new { url = $"{Settings.IMAGES_ENDPOINT}/{Settings.CONTAINER_FACES}/{inputImage.Name}" },
                log);

            await inputImage.FetchAttributesAsync();

            if (result.Count == 0)
            {
                log.Info($"Didn't detect faces => image will be delete down the pipe");
                return;
            }

            outputQueue.Add((new ProcessedImage
            {
                ImageName = inputImage.Name,
                ClientId = Int32.Parse(inputImage.Metadata["clientId"]),
                Faces = result
                    .Select(r => new DetectedFace
                    {
                        FaceId = r.FaceId,
                        FaceRectangle = r.FaceRectangle,
                        Scores = r.FaceAttributes.Emotion
                    })
                    .ToArray()
            }).ToJson());
        }
    }
}