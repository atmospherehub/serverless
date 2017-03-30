#r "System.Drawing"

using System;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using CM = System.Configuration.ConfigurationManager;

public const string CONTAINER_NAME_OUTPUT = "rectangles";
public const string CONTAINER_NAME_INPUT = "faces";
public static readonly ImageCodecInfo codecInfo = ImageCodecInfo.GetImageEncoders()
        .First(e => e.FormatID == ImageFormat.Jpeg.Guid);
public static readonly CloudBlobClient blobClient = CloudStorageAccount
    .Parse(CM.AppSettings["funcatmosphere_STORAGE"]).CreateCloudBlobClient();     

public static void Run(string message, TraceWriter log)
{
    log.Info($"Triggered StoreRectangles by: {message}");
    var face = JsonConvert.DeserializeObject<Face>(message);

    using (var inputStream = new MemoryStream())
    {
        downloadBlob(face.ImageName, inputStream);
        inputStream.Seek(0, SeekOrigin.Begin);
        log.Info($"Loaded image from blob in size of {inputStream.Length}");

        using (var image = Image.FromStream(inputStream))
        {
            using (var graphics = Graphics.FromImage(image))
            {
                var rectangle = new Rectangle(face.FaceRectangle.Left, face.FaceRectangle.Top, 
                    face.FaceRectangle.Width, face.FaceRectangle.Height);
                log.Info($"Drawing rectangle {rectangle}");
                graphics.DrawRectangle(Pens.Lime, rectangle);
            }

            using (var encoders = new EncoderParameters())
            {
                encoders.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
                using (var outputStream = new MemoryStream())
                {
                    image.Save(outputStream, codecInfo, encoders);
                    outputStream.Seek(0, SeekOrigin.Begin);

                    var fileName = $"{face.Id.ToString("D")}.jpg";
                    uploadBlob(fileName, outputStream);
                    log.Info($"Uploaded image {fileName} blob in size of {outputStream.Length}");
                }
            }
        }
    }
}

private static void downloadBlob(string fileName, Stream stream)
{
    var container = blobClient.GetContainerReference(CONTAINER_NAME_INPUT);
    var block = container.GetBlockBlobReference(fileName);
    block.DownloadToStream(stream);
}

private static void uploadBlob(string fileName, Stream stream)
{
    var container = blobClient.GetContainerReference(CONTAINER_NAME_OUTPUT);
    var block = container.GetBlockBlobReference(fileName);
    block.UploadFromStream(stream);
}

public class Face
{
    public Guid Id { get; set; }
    public string ImageName { get; set; }
    public Rectangle FaceRectangle { get; set; }

    public class Rectangle
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
