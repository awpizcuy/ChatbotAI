// Pastikan namespace ini cocok dengan path folder Anda
namespace MyChatbotBackend.DTOs;

public class FeedbackDto
{
    public required List<ChatMessage> ConversationHistory { get; set; }
    public required string UserQuestion { get; set; }
    public required string AiResponse { get; set; }
    public required string Rating { get; set; }
}

public class ChatMessage
{
    public required string Role { get; set; }
    public required string Content { get; set; }
}