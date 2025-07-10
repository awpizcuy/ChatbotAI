// MyChatbotBackend/Services/MetaChatService.cs

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json; // Diperlukan untuk JsonException
using System.Text.Json.Serialization; // Diperlukan untuk JsonPropertyName
using MyChatbotBackend.Models; // Diperlukan untuk ExternalApiResponse
using static MyChatbotBackend.Controllers.ChatController; // Diperlukan untuk ChatMessage

namespace MyChatbotBackend.Services
{
    // Pastikan IAiChatService didefinisikan di file terpisah jika tidak ada
    // public interface IAiChatService { Task<string> GetChatResponseAsync(List<ChatMessage> chatHistory); }

    public class MetaChatService : IAiChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _metaApiUrl;
        private readonly string _metaApiKey;
        private readonly string _metaModelName;

        public MetaChatService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;

            _metaApiUrl = _configuration["Meta:Url"] ?? 
                          throw new ArgumentNullException("Meta:Url is not configured in appsettings.json.");
            _metaApiKey = _configuration["Meta:ApiKey"] ?? // <--- PERBAIKAN DI SINI: UBAH KE "Meta:ApiKey"
                          throw new ArgumentNullException("Meta:ApiKey is not configured in appsettings.json.");
            _metaModelName = _configuration["Meta:ModelName"] ?? "meta-llama/llama-4-scout";

            // Mengatur header otorisasi untuk API Meta (umumnya Bearer Token)
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_metaApiKey}");
        }

        public async Task<string> GetChatResponseAsync(List<ChatMessage> chatHistory)
        {
            Console.WriteLine("DEBUG: Menggunakan MetaChatService untuk menghubungi API Meta.");

            var metaPayload = new
            {
                messages = chatHistory,
                model = _metaModelName,
                temperature = 0.7,
                max_tokens = 1000,
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(_metaApiUrl, metaPayload);
                
                // Tambahkan logging detail respons jika diperlukan untuk debug
                string rawResponseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"DEBUG MetaService Raw Response Status: {response.StatusCode}");
                Console.WriteLine($"DEBUG MetaService Raw Response Content: {rawResponseContent.Substring(0, Math.Min(rawResponseContent.Length, 500))}");

                response.EnsureSuccessStatusCode();

                var metaResponse = System.Text.Json.JsonSerializer.Deserialize<ExternalApiResponse>(rawResponseContent);
                return metaResponse?.Choices?[0]?.Message?.Content ?? "Maaf, tidak ada respons dari Meta AI.";
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"ERROR MetaChatService (HttpRequestException): Gagal menghubungi Meta AI: {ex.Message}");
                throw new Exception($"Gagal menghubungi Meta AI: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"ERROR MetaChatService (JsonException): {ex.Message}");
                throw new Exception($"Gagal mengurai respons dari Meta AI: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR MetaChatService (Umum): {ex.Message}");
                throw new Exception($"Kesalahan tidak terduga di MetaChatService: {ex.Message}", ex);
            }
        }
    }
}