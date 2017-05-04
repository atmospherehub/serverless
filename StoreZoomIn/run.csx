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

public const string CONTAINER_NAME_OUTPUT = "zoomin";
public const string CONTAINER_NAME_INPUT = "faces";
public static readonly ImageCodecInfo codecInfo = ImageCodecInfo.GetImageEncoders()
        .First(e => e.FormatID == ImageFormat.Jpeg.Guid);
public static readonly CloudBlobClient blobClient = CloudStorageAccount
    .Parse(CM.AppSettings["funcatmosphere_STORAGE"]).CreateCloudBlobClient();
private const int MAX_WIDTH = 600;
private const bool SHOULD_DRAW_GREEN_RECT = false;

public static void Run(string message, TraceWriter log)
{
    log.Info($"Triggered StoreZoomIn by: {message}");
    var face = JsonConvert.DeserializeObject<Face>(message);

    using (var inputStream = downloadBlob(face.ImageName))
    {
        log.Info($"Loaded image from blob in size of {inputStream.Length}");

        using(var outputStream = zoom(face.FaceRectangle, inputStream))
        {
            var fileName = $"{face.Id.ToString("D")}.jpg";
            uploadBlob(fileName, outputStream);
            log.Info($"Uploaded image {fileName} blob in size of {outputStream.Length}");
        }
    }
}

private static Stream zoom(Face.Rectangle faceArea, Stream inputStream)
{
    var center = new Point(
        faceArea.Left + faceArea.Width / 2,
        faceArea.Top + faceArea.Height / 2);

    using (var sourceImage = Image.FromStream(inputStream))
    {
        if (SHOULD_DRAW_GREEN_RECT)
        {
            using (var graphics = Graphics.FromImage(sourceImage))
                graphics.DrawRectangle(Pens.Lime, new Rectangle(faceArea.Left, faceArea.Top, faceArea.Width, faceArea.Height));
        }

        var zoomArea = getZoomArea(center, sourceImage.Size);
        using (var targetImage = new Bitmap(zoomArea.Width, zoomArea.Height))
        {
            using (var graphics = Graphics.FromImage(targetImage))
                graphics.DrawImage(
                    sourceImage,
                    new Rectangle(new Point(), zoomArea.Size),
                    zoomArea,
                    GraphicsUnit.Pixel);

            using (var encoders = new EncoderParameters())
            {
                encoders.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
                var outputStream = new MemoryStream();
                targetImage.Save(outputStream, codecInfo, encoders);
                outputStream.Seek(0, SeekOrigin.Begin);
                return outputStream;
            }
        }
    }
}

private static Rectangle getZoomArea(Point center, Size imageSize)
{
    var ratio = (double)imageSize.Width / (double)imageSize.Height;
    var zoomSize = new Size(MAX_WIDTH, (int)Math.Round(MAX_WIDTH / ratio, 0));

    if (zoomSize.Width > imageSize.Width || zoomSize.Height > imageSize.Height)
        throw new InvalidOperationException("Image smaller than zoom-in area");

    var leftTop = new Point();
    if (center.X - zoomSize.Width / 2 > 0)
        leftTop.X = center.X - zoomSize.Width / 2;

    if (center.X + zoomSize.Width / 2 > imageSize.Width)
        leftTop.X = imageSize.Width - zoomSize.Width;

    if (center.Y - zoomSize.Height / 2 > 0)
        leftTop.Y = center.Y - zoomSize.Height / 2;

    if (center.Y + zoomSize.Height / 2 > imageSize.Height)
        leftTop.Y = imageSize.Height - zoomSize.Height;
    return new Rectangle(leftTop, zoomSize);
}


private static Stream downloadBlob(string fileName)
{
    var container = blobClient.GetContainerReference(CONTAINER_NAME_INPUT);
    var block = container.GetBlockBlobReference(fileName);
    var stream = new MemoryStream();
    block.DownloadToStream(stream);
    stream.Seek(0, SeekOrigin.Begin);
    return stream;
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
