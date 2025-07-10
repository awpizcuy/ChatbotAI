// MyChatbotBackend/Services/OllamaChatService.cs
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using static MyChatbotBackend.Controllers.ChatController; // Untuk mengakses ChatMessage dan OllamaResponse

namespace MyChatbotBackend.Services
{
    public class OllamaChatService : IAiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _ollamaModelName;

        public OllamaChatService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _ollamaModelName = _configuration["OllamaApi:ModelName"] ?? "gemma3"; // Pastikan ini mengambil model dari appsettings
            
            // Set BaseAddress HttpClient jika belum diatur di Program.cs untuk layanan spesifik ini
            // Jika HttpClient sudah dikonfigurasi sebagai Singleton dengan BaseAddress di Program.cs, ini opsional
            _httpClient.BaseAddress = new Uri(_configuration["OllamaApi:Url"] ?? "http://localhost:11434");
        }

        public async Task<string> GetChatResponseAsync(List<ChatMessage> chatHistory)
        {
            var ollamaPayload = new
            {
                model = _ollamaModelName,
                messages = chatHistory,
                stream = false
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/chat", ollamaPayload);
                response.EnsureSuccessStatusCode(); // Ini akan throw HttpRequestException jika status code non-sukses

                var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                return ollamaResponse?.Message?.Content ?? "Maaf, tidak ada respons dari Ollama.";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"ERROR OllamaChatService (HttpRequestException): {ex.Message}");
                throw new Exception($"Gagal menghubungi Ollama: {ex.Message}", ex); // Re-throw sebagai Exception generik
            }
            catch (System.Text.Json.JsonException ex)
            {
                Console.WriteLine($"ERROR OllamaChatService (JsonException): {ex.Message}");
                throw new Exception($"Gagal mengurai respons dari Ollama: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR OllamaChatService (Umum): {ex.Message}");
                throw new Exception($"Kesalahan tidak terduga di OllamaChatService: {ex.Message}", ex);
            }
        }
    }
}