using System.Net;
using System.Threading;
using System.Net.Http.Headers;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage request, Stream outputBlob, TraceWriter log)
{
    log.Info($"Triggered FacesUpload");

    if (!request.Content.IsMimeMultipartContent())
        return request.CreateResponse(HttpStatusCode.UnsupportedMediaType);

    await request.Content.ReadAsMultipartAsync(new MultipartFormDataStreamProvider(outputBlob));
    return request.CreateResponse(HttpStatusCode.OK);
}

public class MultipartFormDataStreamProvider : MultipartStreamProvider
{
    private Stream _stream;

    public MultipartFormDataStreamProvider(Stream stream)
    {
        this._stream = stream;
    }

    public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
    {
        return _stream;
    }
}