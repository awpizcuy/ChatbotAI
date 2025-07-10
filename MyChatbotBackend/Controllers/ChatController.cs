// MyChatbotBackend/Controllers/ChatController.cs

using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using MyChatbotBackend.Models; // Untuk DocumentChunk, UserQuestion
using System.Linq; // Untuk LINQ
using MyChatbotBackend.Data; // Untuk AppDbContext
using Microsoft.EntityFrameworkCore; // Untuk DbContext
using MyChatbotBackend.Services; // Untuk IAiChatService, OllamaChatService, dll.
using Microsoft.Extensions.DependencyInjection; // Untuk GetRequiredService dari IServiceProvider

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
        private readonly AppDbContext _dbContext;
        private readonly IServiceProvider _serviceProvider;

        public ChatController(HttpClient httpClient, IConfiguration configuration, List<DocumentChunk> vectorStore, AppDbContext dbContext, IServiceProvider serviceProvider)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _vectorStore = vectorStore;
            _dbContext = dbContext;
            _serviceProvider = serviceProvider;

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
            [JsonPropertyName("model")]
            public string Model { get; set; } = "ollama";
        }

        public class OllamaEmbeddingRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; } = string.Empty;
            [JsonPropertyName("prompt")]
            public string Prompt { get; set; } = string.Empty;
        }

        public class OllamaEmbeddingResponse
        {
            [JsonPropertyName("embedding")]
            public List<float>? Embedding { get; set; }
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

        // --- Fungsi Pembantu ---
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

        private bool IsChitChat(string question)
        {
            var lowerCaseQuestion = question.ToLowerInvariant();
            var chitChatKeywords = new List<string>
            {
                "halo", "hai", "selamat pagi", "selamat siang", "selamat sore", "selamat malam",
                "terima kasih", "makasih", "ok", "oke", "siap", "boleh"
            };
            return chitChatKeywords.Any(keyword => lowerCaseQuestion.Contains(keyword));
        }

        private IAiChatService GetChatService(string modelName)
        {
            return modelName.ToLower() switch
            {
                "deepseek" => _serviceProvider.GetRequiredService<DeepseekChatService>(),
                "meta" => _serviceProvider.GetRequiredService<MetaChatService>(),
                _ => _serviceProvider.GetRequiredService<OllamaChatService>(),
            };
        }

        // --- Endpoint Chatbot Utama ---
        [HttpPost]
        public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest chatRequest)
        {
            if (string.IsNullOrWhiteSpace(chatRequest.Message))
            {
                return BadRequest(new { error = "Pesan tidak boleh kosong." });
            }

            UserQuestion? userQuestionToSave = null;
            ActionResult<ChatResponse> finalResult; // Deklarasi variabel untuk hasil akhir

            // --- Logika Penyimpanan Pertanyaan Pengguna (Bagian 1: Simpan pertanyaan awal) ---
            try
            {
                userQuestionToSave = new UserQuestion
                {
                    QuestionText = chatRequest.Message,
                    AiResponse = "Menunggu respons dari AI...",
                    Timestamp = DateTime.Now
                };
                _dbContext.UserQuestions.Add(userQuestionToSave);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"DEBUG DB: Pertanyaan awal disimpan: '{userQuestionToSave.QuestionText}' (Id: {userQuestionToSave.Id})");
            }
            catch (Exception dbEx)
            {
                Console.WriteLine($"ERROR DB: Gagal menyimpan pertanyaan awal ke database: {dbEx.Message}");
                return StatusCode(500, new { error = $"Gagal menyimpan pertanyaan pengguna: {dbEx.Message}" });
            }
            // --- Akhir Logika Penyimpanan Bagian 1 ---


            var currentHistory = new List<ChatMessage>(chatRequest.History);
            currentHistory.Add(new ChatMessage { Role = "user", Content = chatRequest.Message });
            
            // --- Logika Penentuan systemInstruction ---
            string systemInstruction = "";

            // --- Logika Penentuan systemInstruction ---
            if (IsChitChat(chatRequest.Message))
            {
                Console.WriteLine($"DEBUG: Mendeteksi sapaan.");
                // Instruksi untuk sapaan
                systemInstruction = "Anda adalah asisten AI yang ramah, sopan, dan gaul. Pahami berbagai gaya bahasa pengguna (formal, santai, gaul) dan respons Anda harus selalu seperti manusia pada umumnya. Balas sapaan pengguna dengan singkat, ramah, dan gaya bahasa yang santai."; // <--- PERBARUI DI SINI
            }
            else // Ini adalah alur RAG
            {
                // Deklarasikan relevantChunks di scope yang lebih tinggi dalam blok else ini
                IEnumerable<dynamic> relevantChunks = Enumerable.Empty<dynamic>();

                if (_vectorStore != null && _vectorStore.Any())
                {
                    var userQueryEmbedding = await GetEmbeddingAsync(chatRequest.Message);

                    if (userQueryEmbedding != null && userQueryEmbedding.Any())
                    {
                        relevantChunks = _vectorStore
                            .Select(chunk => new
                            {
                                Chunk = chunk,
                                Similarity = CalculateCosineSimilarity(userQueryEmbedding, chunk.Embedding)
                            })
                            .OrderByDescending(x => x.Similarity)
                            .Take(5)
                            .ToList();

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

                        var filteredRelevantChunks = relevantChunks.Where(x => x.Similarity > 0.5).ToList();

                        if (filteredRelevantChunks.Any())
                        {
                            Console.WriteLine($"Menemukan {filteredRelevantChunks.Count} chunk relevan (setelah filter).");
                            var context = string.Join("\n\n---\n\n", filteredRelevantChunks.Select(rc => rc.Chunk.Content));
                            
                            // Prompt ketika ada konteks relevan
                            systemInstruction = $"Anda adalah asisten AI yang ramah, sopan, dan gaul, seperti manusia pada umumnya. Pahami berbagai gaya bahasa pertanyaan pengguna (formal, santai, gaul, pertanyaan yang diulang dengan gaya berbeda). Tugas Anda adalah menjawab pertanyaan HANYA dan SEPENUHNYA berdasarkan konteks dokumen yang diberikan di bawah ini. Gunakan bahasa yang adaptif (bisa formal, santai, atau gaul) sesuai konteks percakapan, tetapi selalu ramah dan informatif. JANGAN gunakan pengetahuan umum Anda. Jika jawaban tidak ada di dalam dokumen, atau tidak dapat dijawab secara langsung dari dokumen, katakan 'Yah, maaf banget nih, info itu kayaknya nggak ada di dokumen yang aku punya. Coba tanya yang lain yang masih nyambung sama dokumennya ya!'\n\nDokumen:\n{context}\n\n"; // <--- PERBARUI DI SINI
                        } else {
                            Console.WriteLine("Tidak ada chunk relevan yang ditemukan setelah filter, memaksa AI untuk menjawab di luar jangkauan.");
                            // Prompt ketika TIDAK ada konteks relevan
                            systemInstruction = "Anda adalah asisten AI yang ramah, sopan, dan gaul, seperti manusia pada umumnya. Pahami berbagai gaya bahasa pertanyaan pengguna (formal, santai, gaul, pertanyaan yang diulang dengan gaya berbeda). Tugas Anda adalah HANYA menjawab pertanyaan berdasarkan dokumen yang telah diberikan. Anda TIDAK memiliki akses ke pengetahuan umum atau internet. Jika pertanyaan pengguna tidak dapat dijawab SAMA SEKALI dari dokumen yang Anda miliki, Anda HARUS membalas: 'Yah, maaf banget nih, pertanyaanmu di luar jangkauan pengetahuan aku berdasarkan dokumen yang ada. Coba tanya yang lain aja ya!'"; // <--- PERBARUI DI SINI
                        }
                    } else {
                        Console.WriteLine("Gagal mendapatkan embedding untuk query pengguna, memaksa AI untuk menjawab di luar jangkauan.");
                        systemInstruction = "Anda adalah asisten AI yang ramah, sopan, dan gaul, seperti manusia pada umumnya. Pahami berbagai gaya bahasa pertanyaan pengguna (formal, santai, gaul, pertanyaan yang diulang dengan gaya berbeda). Tugas Anda adalah HANYA menjawab pertanyaan berdasarkan dokumen yang telah diberikan. Anda TIDAK memiliki akses ke pengetahuan umum atau internet. Jika pertanyaan pengguna tidak dapat dijawab SAMA SEKALI dari dokumen yang Anda miliki, Anda HARUS membalas: 'Maaf, pertanyaan Anda di luar jangkauan pengetahuan saya berdasarkan dokumen yang tersedia.'"; // <--- PERBARUI DI SINI
                    }
                } else {
                    Console.WriteLine("Vector store kosong atau null, memaksa AI untuk menjawab di luar jangkauan.");
                    systemInstruction = "Anda adalah asisten AI yang ramah, sopan, dan gaul, seperti manusia pada umumnya. Pahami berbagai gaya bahasa pertanyaan pengguna (formal, santai, gaul, pertanyaan yang diulang dengan gaya berbeda). Tugas Anda adalah HANYA menjawab pertanyaan berdasarkan dokumen yang telah diberikan. Anda TIDAK memiliki akses ke pengetahuan umum atau internet. Jika pertanyaan pengguna tidak dapat menjawab SAMA SEKALI dari dokumen yang Anda miliki, Anda HARUS membalas: 'Maaf, pertanyaan Anda di luar jangkauan pengetahuan saya berdasarkan dokumen yang tersedia.'"; // <--- PERBARUI DI SINI
                }

            }
            // --- AKHIR Logika Penentuan systemInstruction ---
            
            // SELALU sisipkan system message ini di awal currentHistory
            currentHistory.Insert(0, new ChatMessage { Role = "system", Content = systemInstruction });

            // --- Panggilan AI melalui service dinamis ---
            IAiChatService chatService = GetChatService(chatRequest.Model);
            string aiMessageContent = "Maaf, tidak ada respons dari AI.";

            try
            {
                aiMessageContent = await chatService.GetChatResponseAsync(currentHistory);
                
                currentHistory.Add(new ChatMessage { Role = "assistant", Content = aiMessageContent });

                // --- Logika Penyimpanan Jawaban AI (Bagian 2: Update jawaban AI) ---
                if (userQuestionToSave != null)
                {
                    userQuestionToSave.AiResponse = aiMessageContent;
                    await _dbContext.SaveChangesAsync(); 
                    Console.WriteLine($"DEBUG DB: Jawaban AI diperbarui untuk Id {userQuestionToSave.Id}.");
                }
                // --- Akhir Logika Penyimpanan Bagian 2 ---

                finalResult = Ok(new ChatResponse // <-- SET HASIL
                {
                    Reply = aiMessageContent,
                    History = currentHistory
                });
            }
            catch (Exception ex) // Tangkap semua exception dari panggilan chatService
            {
                Console.WriteLine($"Error komunikasi atau pemrosesan AI: {ex.Message}");
                if (userQuestionToSave != null) { 
                    userQuestionToSave.AiResponse = $"Error: {ex.Message}";
                    await _dbContext.SaveChangesAsync();
                }
                finalResult = StatusCode(500, new { error = $"Gagal mendapatkan respons dari AI: {ex.Message}" }); // <-- SET HASIL
            }
            return finalResult; // <--- KEMBALIKAN HASIL DI AKHIR
        }
    }
}