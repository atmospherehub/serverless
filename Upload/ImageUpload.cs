using Common;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Data.SqlClient;
using Dapper;

namespace Upload
{
    public static class ImageUpload
    {
        [FunctionName(nameof(ImageUpload))]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage request,
            [Blob(blobPath: "faces/{rand-guid}.jpg", Connection = Settings.STORAGE_CONN_NAME)] Stream outputBlob,
            TraceWriter log)
        {
            log.Info($"Triggered '{nameof(ImageUpload)}'");

            if (!request.Content.IsMimeMultipartContent())
                return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);

            if (String.IsNullOrEmpty(request.Headers.Authorization?.Parameter))
                return request.CreateResponse(HttpStatusCode.Unauthorized);
            
            if (!Guid.TryParse(request.Headers.Authorization?.Parameter, out var token))
                return request.CreateResponse(HttpStatusCode.Forbidden);
            
            var clientId = await validateToken(token);
            if (!clientId.HasValue)
                return request.CreateResponse(HttpStatusCode.Forbidden);

            log.Info($"Detected client {clientId}");

            var multipartData = await request.Content.ReadAsMultipartAsync();
            if (multipartData.Contents.Count != 1)
                return request.CreateResponse(HttpStatusCode.BadRequest);

            await multipartData.Contents[0].CopyToAsync(outputBlob);
            return request.CreateResponse(HttpStatusCode.OK);
        }

        private static async Task<int?> validateToken(Guid token)
        {
            using (var connection = new SqlConnection(Settings.SQL_CONN_STRING))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int?>(
                    @"SELECT Id FROM [dbo].[Clients] WHERE Token = @token AND IsDisabled = 0",
                    new { token });
            }
        }
    }
}