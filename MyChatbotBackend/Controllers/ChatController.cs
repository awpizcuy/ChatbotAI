// MyChatbotBackend/Controllers/ChatController.cs

using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using MyChatbotBackend.Models;
using System.Linq; // Untuk LINQ

namespace MyChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly List<DocumentChunk> _vectorStore;
        private readonly string _embeddingModelName;

        public ChatController(HttpClient httpClient, IConfiguration configuration, List<DocumentChunk> vectorStore)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _vectorStore = vectorStore;

            _httpClient.BaseAddress = new Uri(_configuration["OllamaApi:Url"] ?? "http://localhost:11434");
            _embeddingModelName = _configuration["OllamaApi:EmbeddingModelName"] ?? "nomic-embed-text";
        }

        // --- Model (DTOs) ---
        public class ChatMessage
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = string.Empty;

            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;
        }

        public class ChatRequest
        {
            [JsonPropertyName("message")]
            public string Message { get; set; } = string.Empty;

            [JsonPropertyName("history")]
            public List<ChatMessage> History { get; set; } = new List<ChatMessage>();
        }

        public class OllamaResponse
        {
            [JsonPropertyName("message")]
            public ChatMessage? Message { get; set; }
        }

        public class ChatResponse
        {
            [JsonPropertyName("reply")]
            public string Reply { get; set; } = string.Empty;

            [JsonPropertyName("history")]
            public List<ChatMessage> History { get; set; } = new List<ChatMessage>();
        }

        // --- Fungsi Pembantu RAG ---

        // Menghitung Cosine Similarity antara dua vektor
        private static double CalculateCosineSimilarity(List<float> vector1, List<float> vector2)
        {
            if (vector1 == null || vector2 == null || vector1.Count == 0 || vector1.Count != vector2.Count)
            {
                return 0.0;
            }

            double dotProduct = 0.0;
            double magnitude1 = 0.0;
            double magnitude2 = 0.0;

            for (int i = 0; i < vector1.Count; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0.0;
            }

            return dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
        }

        // Mendapatkan embedding dari Ollama untuk sebuah prompt
        private async Task<List<float>?> GetEmbeddingAsync(string prompt)
        {
            var embeddingRequest = new OllamaEmbeddingRequest
            {
                Model = _embeddingModelName,
                Prompt = prompt
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/embeddings", embeddingRequest);
                response.EnsureSuccessStatusCode();
                var ollamaEmbedding = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
                return ollamaEmbedding?.Embedding;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Gagal mendapatkan embedding untuk prompt '{prompt.Substring(0, Math.Min(prompt.Length, 50))}...': {ex.Message}");
                return null;
            }
        }

        // --- Endpoint Chatbot Utama ---
        [HttpPost]
        public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest chatRequest)
        {
            if (string.IsNullOrWhiteSpace(chatRequest.Message))
            {
                return BadRequest(new { error = "Pesan tidak boleh kosong." });
            }

            var currentHistory = new List<ChatMessage>(chatRequest.History);
            currentHistory.Add(new ChatMessage { Role = "user", Content = chatRequest.Message });

            string systemInstruction = "";

            // --- Logika RAG: Ambil Konteks dari Dokumen ---
            if (_vectorStore != null && _vectorStore.Any())
            {
                var userQueryEmbedding = await GetEmbeddingAsync(chatRequest.Message);

                if (userQueryEmbedding != null && userQueryEmbedding.Any())
                {
                    var relevantChunks = _vectorStore
                        .Select(chunk => new
                        {
                            Chunk = chunk,
                            Similarity = CalculateCosineSimilarity(userQueryEmbedding, chunk.Embedding)
                        })
                        .OrderByDescending(x => x.Similarity) // Urutkan berdasarkan kesamaan
                        // .Where(x => x.Similarity > 0.7) // <--- KOMENTARI ATAU UBAH THRESHOLD INI
                        .Take(5) // <--- Ambil 5 chunk teratas (bukan hanya yang > threshold)
                        .ToList();

                    // --- LOGGING DIAGNOSTIK BARU ---
                    Console.WriteLine($"DEBUG RAG: Query: '{chatRequest.Message}'");
                    if (relevantChunks.Any())
                    {
                        Console.WriteLine("DEBUG RAG: Chunk relevan (teratas):");
                        foreach (var rc in relevantChunks)
                        {
                            Console.WriteLine($"- Sim: {rc.Similarity:F4}, Content: {rc.Chunk.Content.Substring(0, Math.Min(rc.Chunk.Content.Length, 100))}...");
                        }
                    } else {
                        Console.WriteLine("DEBUG RAG: Tidak ada chunk yang ditemukan untuk dipertimbangkan.");
                    }
                    // --- AKHIR LOGGING DIAGNOSTIK BARU ---

                    // Filter ulang chunk yang relevan berdasarkan threshold yang lebih rendah atau fleksibel
                    var filteredRelevantChunks = relevantChunks.Where(x => x.Similarity > 0.5).ToList(); // <--- COBA THRESHOLD LEBIH RENDAH (0.5 atau 0.6)

                    if (filteredRelevantChunks.Any())
                    {
                        Console.WriteLine($"Menemukan {filteredRelevantChunks.Count} chunk relevan (setelah filter).");
                        var context = string.Join("\n\n---\n\n", filteredRelevantChunks.Select(rc => rc.Chunk.Content));
                        
                        // Prompt ketat ketika ada konteks relevan
                        systemInstruction = $"Anda adalah asisten AI. Tugas Anda adalah menjawab pertanyaan HANYA dan SEPENUHNYA berdasarkan konteks dokumen yang diberikan di bawah ini. JANGAN gunakan pengetahuan umum Anda. Jika jawaban tidak ada di dalam dokumen, atau tidak dapat dijawab secara langsung dari dokumen, katakan 'Maaf, saya tidak dapat menemukan informasi relevan di dokumen yang diberikan. Silakan ajukan pertanyaan lain yang terkait dengan dokumen.'\n\nDokumen:\n{context}\n\n";
                    } else {
                        Console.WriteLine("Tidak ada chunk relevan yang ditemukan setelah filter, memaksa AI untuk menjawab di luar jangkauan.");
                        // Prompt sangat ketat ketika TIDAK ada konteks relevan
                        systemInstruction = "Anda adalah asisten AI. Tugas Anda adalah HANYA menjawab pertanyaan berdasarkan dokumen yang telah diberikan. Anda TIDAK memiliki akses ke pengetahuan umum atau internet. Jika pertanyaan pengguna tidak dapat dijawab SAMA SEKALI dari dokumen yang Anda miliki, Anda HARUS membalas: 'Maaf, pertanyaan Anda di luar jangkauan pengetahuan saya berdasarkan dokumen yang tersedia.'";
                    }
                } else {
                    Console.WriteLine("Gagal mendapatkan embedding untuk query pengguna, memaksa AI untuk menjawab di luar jangkauan.");
                    systemInstruction = "Anda adalah asisten AI. Tugas Anda adalah HANYA menjawab pertanyaan berdasarkan dokumen yang telah diberikan. Anda TIDAK memiliki akses ke pengetahuan umum atau internet. Jika pertanyaan pengguna tidak dapat dijawab SAMA SEKALI dari dokumen yang Anda miliki, Anda HARUS membalas: 'Maaf, pertanyaan Anda di luar jangkauan pengetahuan saya berdasarkan dokumen yang tersedia.'";
                }
            } else {
                Console.WriteLine("Vector store kosong atau null, memaksa AI untuk menjawab di luar jangkauan.");
                systemInstruction = "Anda adalah asisten AI. Tugas Anda adalah HANYA menjawab pertanyaan berdasarkan dokumen yang telah diberikan. Anda TIDAK memiliki akses ke pengetahuan umum atau internet. Jika pertanyaan pengguna tidak dapat dijawab SAMA SEKALI dari dokumen yang Anda miliki, Anda HARUS membalas: 'Maaf, pertanyaan Anda di luar jangkauan pengetahuan saya berdasarkan dokumen yang tersedia.'";
            }
            // --- Akhir Logika RAG ---
            
            // SELALU sisipkan system message ini di awal currentHistory
            currentHistory.Insert(0, new ChatMessage { Role = "system", Content = systemInstruction });

            // Siapkan payload untuk API Ollama
            var ollamaPayload = new
            {
                model = _configuration["OllamaApi:ModelName"] ?? "gemma3",
                messages = currentHistory,
                stream = false
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/chat", ollamaPayload);
                response.EnsureSuccessStatusCode();

                var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                var aiMessageContent = ollamaResponse?.Message?.Content ?? "Maaf, tidak ada respons dari AI.";

                currentHistory.Add(new ChatMessage { Role = "assistant", Content = aiMessageContent });

                return Ok(new ChatResponse
                {
                    Reply = aiMessageContent,
                    History = currentHistory
                });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error komunikasi dengan Ollama API: {ex.Message}");
                return StatusCode(500, new { error = $"Gagal mendapatkan respons dari AI (Jaringan): {ex.Message}" });
            }
            catch (System.Text.Json.JsonException ex)
            {
                Console.WriteLine($"Error deserialisasi respons Ollama: {ex.Message}");
                return StatusCode(500, new { error = "Gagal memproses respons dari AI." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error tidak terduga: {ex.Message}");
                return StatusCode(500, new { error = $"Terjadi kesalahan tak terduga: {ex.Message}" });
            }
        }
    }
}