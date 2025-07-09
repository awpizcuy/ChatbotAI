using MyChatbotBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyChatbotBackend.Services
{
    public interface IYourAIService
    {
        Task<string> GenerateImprovedAnswerAsync(List<ChatMessage> conversationHistory, string dislikedResponse);
    }
}