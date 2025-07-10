// File: Services/IAiChatService.cs

using System.Collections.Generic;
using System.Threading.Tasks;
using MyChatbotBackend.Controllers;

namespace MyChatbotBackend.Services
{
    public interface IAiChatService
    {
        Task<string> GetChatResponseAsync(List<ChatController.ChatMessage> history);
    }
}