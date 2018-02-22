namespace Functions.Upload.Models
{
    public class ProcessedImage
    {
        public string ImageName { get; set; }

        public DetectedFace[] Faces { get; set; }

        public int ClientId { get; set; }
    }
}
