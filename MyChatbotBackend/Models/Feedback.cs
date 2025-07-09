using System; 
using System.ComponentModel.DataAnnotations;

namespace MyChatbotBackend.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        public required string UserQuestion { get; set; }

        public required string AIResponse { get; set; }

        public required string ConversationHistory { get; set; }

        public required string Rating { get; set; }

        public required DateTime CreatedAt { get; set; } // <-- Dibuat 'required' agar konsisten
    }
}