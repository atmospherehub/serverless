namespace Functions.Upload.Models
{
    public class ProcessedFace : DetectedFace
    {
        public string ImageName { get; set; }

        public int ClientId { get; set; }
    }
}
