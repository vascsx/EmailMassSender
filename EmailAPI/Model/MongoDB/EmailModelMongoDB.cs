﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EmailAPI.Model.MongoDB
{
    public class EmailModelMongoDB
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
