// MyChatbotBackend/Services/DeepseekChatService.cs

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyChatbotBackend.Models; // Untuk ExternalApiResponse
using static MyChatbotBackend.Controllers.ChatController; // Untuk ChatMessage

namespace MyChatbotBackend.Services
{
    public class DeepseekChatService : IAiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _deepseekApiUrl;
        private readonly string _deepseekApiKey;
        private readonly string _deepseekModelName;

        public DeepseekChatService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            // --- BAGIAN INI SUDAH DIPERBAIKI. PASTIKAN SESUAI ---
            _deepseekApiUrl = _configuration["OpenRouter:Url"] ?? // Membaca dari "OpenRouter:Url"
                              throw new ArgumentNullException("OpenRouter:Url for Deepseek API is not configured.");
            _deepseekApiKey = _configuration["OpenRouter:ApiKey"] ?? // Membaca dari "OpenRouter:ApiKey"
                              throw new ArgumentNullException("OpenRouter:ApiKey for Deepseek API is not configured.");
            _deepseekModelName = _configuration["OpenRouter:ModelName"] ?? "deepseek/deepseek-chat"; // Membaca dari "OpenRouter:ModelName"

            // Mengatur header otorisasi untuk API DeepSeek
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_deepseekApiKey}");
        }

        public async Task<string> GetChatResponseAsync(List<ChatMessage> chatHistory)
        {
            Console.WriteLine("DEBUG: Menggunakan DeepseekChatService untuk menghubungi API DeepSeek.");

            var deepseekPayload = new
            {
                messages = chatHistory,
                model = _deepseekModelName,
                temperature = 0.7,
                max_tokens = 1000,
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(_deepseekApiUrl, deepseekPayload);

                string rawResponseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"DEBUG DeepseekService Raw Response Status: {response.StatusCode}");
                Console.WriteLine($"DEBUG DeepseekService Raw Response Content: {rawResponseContent.Substring(0, Math.Min(rawResponseContent.Length, 500))}");

                response.EnsureSuccessStatusCode();

                var deepseekResponse = System.Text.Json.JsonSerializer.Deserialize<ExternalApiResponse>(rawResponseContent);
                return deepseekResponse?.Choices?[0]?.Message?.Content ?? "Maaf, tidak ada respons dari DeepSeek AI.";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"ERROR DeepseekChatService (HttpRequestException): {ex.Message}");
                throw new Exception($"Gagal menghubungi DeepSeek AI: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"ERROR DeepseekChatService (JsonException): {ex.Message}");
                throw new Exception($"Gagal mengurai respons dari DeepSeek AI: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR DeepseekChatService (Umum): {ex.Message}");
                throw new Exception($"Kesalahan tidak terduga di DeepseekChatService: {ex.Message}", ex);
            }
        }
    }
}