﻿namespace Recognition.Models
{
    public class TaggingMessage
    {
        // Unique identifier (generated by Atmosphere) of the person on the specific image, 
        // this is also a primary key in table `faces`, which means that it is a person 
        // on some image with certain mood scores
        public string FaceId { get; set; }

        // Unique identifier (generated by Slack) of the person on image. This is the value
        // selected by someone when performing tagging
        public string UserId { get; set; }

        // Unique identifier (generated by Slack) of the person who performed tagging
        public string TaggedByUserId { get; set; }

        // Username (generated by Slack) of the person who performed tagging
        public string TaggedByName { get; set; }

        public string ResponseUrl { get; set; }

        public SlackMessage OriginalMessage { get; set; }

        public string MessageTs { get; set; }
    }
}
