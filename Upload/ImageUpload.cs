using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Upload
{
    public static class ImageUpload
    {
        [FunctionName(nameof(ImageUpload))]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage request,
            [Blob(blobPath: "faces/{rand-guid}.jpg", Connection = Settings.STORAGE_CONN_NAME)] Stream outputBlob,
            TraceWriter log)
        {
            log.Info($"Triggered '{nameof(ImageUpload)}'");

            if (!request.Content.IsMimeMultipartContent())
                return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);

            var multipartData = await request.Content.ReadAsMultipartAsync();
            if(multipartData.Contents.Count != 1)
                return request.CreateResponse(HttpStatusCode.BadRequest);

            await multipartData.Contents[0].CopyToAsync(outputBlob);
            return request.CreateResponse(HttpStatusCode.OK);
        }
    }
}