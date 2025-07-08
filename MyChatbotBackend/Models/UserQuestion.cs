// MyChatbotBackend/Models/UserQuestion.cs
using System;

namespace MyChatbotBackend.Models
{
    public class UserQuestion
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string AiResponse { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}