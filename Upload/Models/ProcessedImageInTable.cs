namespace Upload.Models
{
    public class ProcessedImageInTable : ProcessedImage
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
    }
}
