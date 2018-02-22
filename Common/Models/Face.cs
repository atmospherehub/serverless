using System;

namespace Common.Models
{
    public class Face
    {
        public Guid Id { get; set; }
        public string ImageName { get; set; }
        public FaceRectangle FaceRectangle { get; set; }        

        public override string ToString()
        {
            return $"FaceId: {Id}";
        }
    }
}
