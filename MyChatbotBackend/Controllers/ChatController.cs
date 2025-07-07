using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json; // Diperlukan untuk PostAsJsonAsync, biasanya sudah ada di .NET 8
using System.Text.Json;     // Diperlukan untuk JsonException
using System.Text.Json.Serialization; // Diperlukan untuk [JsonPropertyName]
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration; // Diperlukan untuk IConfiguration

namespace MyChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Rute API akan menjadi /api/chat
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        // Constructor untuk injeksi HttpClient dan IConfiguration
        public ChatController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            // Mengatur BaseAddress untuk HttpClient dari appsettings.json
            _httpClient.BaseAddress = new Uri(_configuration["OllamaApi:Url"] ?? "http://localhost:11434");
        }

        // --- Model (DTOs - Data Transfer Objects) ---
        // Model untuk pesan dalam format yang diharapkan oleh Ollama dan frontend
        public class ChatMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty; // "user", "assistant", "system"

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        // Model untuk request yang diterima dari frontend Vue.js
        public class ChatRequest
        {
            [JsonPropertyName("message")]
            public string Message { get; set; } = string.Empty;

            [JsonPropertyName("history")]
            public List<ChatMessage> History { get; set; } = new List<ChatMessage>();
        }

        // Model untuk respons dari Ollama API
        public class OllamaResponse
        {
            [JsonPropertyName("message")]
            public ChatMessage? Message { get; set; } // Nullable jika pesan mungkin tidak selalu ada
        }

        // Model untuk respons yang dikirim kembali ke frontend Vue.js
        public class ChatResponse
        {
            [JsonPropertyName("reply")]
            public string Reply { get; set; } = string.Empty;

            [JsonPropertyName("history")]
            public List<ChatMessage> History { get; set; } = new List<ChatMessage>();
        }

        [HttpPost]
        public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest chatRequest)
        {
            // Verifikasi input
            if (string.IsNullOrWhiteSpace(chatRequest.Message))
            {
                return BadRequest(new { error = "Pesan tidak boleh kosong." });
            }

            // Salin riwayat yang diterima dan tambahkan pesan pengguna
            var currentHistory = new List<ChatMessage>(chatRequest.History);
            currentHistory.Add(new ChatMessage { Role = "user", Content = chatRequest.Message });

            // Siapkan payload untuk dikirim ke API Ollama
            var ollamaPayload = new
            {
                model = _configuration["OllamaApi:ModelName"] ?? "gemma3", // Ambil nama model dari appsettings.json
                messages = currentHistory, // Kirim seluruh riwayat untuk konteks percakapan
                stream = false // Set ke true jika ingin streaming respons (lebih kompleks)
            };

            try
            {
                // Kirim permintaan HTTP POST ke Ollama API
                // Endpoint untuk chat di Ollama adalah "api/chat"
                var response = await _httpClient.PostAsJsonAsync("api/chat", ollamaPayload);

                // Memastikan respons sukses (status code 2xx)
                response.EnsureSuccessStatusCode();

                // Baca respons JSON dari Ollama
                var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>();

                // Ambil konten pesan AI
                var aiMessageContent = ollamaResponse?.Message?.Content ?? "Maaf, tidak ada respons dari AI.";

                // Tambahkan respons AI ke riwayat
                currentHistory.Add(new ChatMessage { Role = "assistant", Content = aiMessageContent });

                // Kirim respons kembali ke frontend Vue.js
                return Ok(new ChatResponse
                {
                    Reply = aiMessageContent,
                    History = currentHistory // Kirim riwayat terbaru kembali
                });
            }
            catch (HttpRequestException ex) // Tangani error jaringan atau HTTP dari Ollama
            {
                Console.WriteLine($"Error komunikasi dengan Ollama API: {ex.Message}");
                return StatusCode(500, new { error = $"Gagal mendapatkan respons dari AI (Jaringan): {ex.Message}" });
            }
            catch (JsonException ex) // Tangani error jika respons JSON dari Ollama tidak valid
            {
                Console.WriteLine($"Error deserialisasi respons Ollama: {ex.Message}");
                return StatusCode(500, new { error = "Gagal memproses respons dari AI." });
            }
            catch (Exception ex) // Tangani error umum lainnya
            {
                Console.WriteLine($"Error tidak terduga: {ex.Message}");
                return StatusCode(500, new { error = $"Terjadi kesalahan tak terduga: {ex.Message}" });
            }
        }
    }
}