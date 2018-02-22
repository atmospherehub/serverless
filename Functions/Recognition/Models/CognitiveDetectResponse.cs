using System.Collections.Generic;

namespace Functions.Recognition.Models
{
    public class CognitiveDetectResponse : List<CognitiveDetectResponse.Face>
    {
        public class Face
        {
            public string FaceId { get; set; }

            public Rectangle FaceRectangle { get; set; }
        }

        public class Rectangle
        {
            public int Top { get; set; }
            public int Left { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}
