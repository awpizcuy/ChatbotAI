// MyChatbotBackend/Models/ExternalApiDtos.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MyChatbotBackend.Controllers;

namespace MyChatbotBackend.Models
{
    // --- Definisi DTO untuk Respons API Eksternal (Contoh: Mirip OpenAI/Gemini) ---
    public class ExternalApiResponse
    {
        [JsonPropertyName("choices")]
        public List<ExternalApiChoice>? Choices { get; set; }
        // Properti lain seperti id, model, usage, dll., bisa ditambahkan jika API Anda mengembalikannya
    }

    public class ExternalApiChoice
    {
        [JsonPropertyName("message")]
        public ChatController.ChatMessage? Message { get; set; } // Menggunakan ChatMessage dari ChatController
        // Properti lain seperti finish_reason, index bisa ditambahkan
    }
    // --- Akhir Definisi DTO ---
}