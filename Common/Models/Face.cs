using System;

namespace Common.Models
{
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

        public override string ToString()
        {
            return $"FaceId: {Id}";
        }
    }
}
