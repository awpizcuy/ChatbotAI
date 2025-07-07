// MyChatbotBackend/Models/DocumentChunk.cs
using System; // <--- TAMBAHKAN INI untuk Guid
using System.Collections.Generic;
using System.Text.Json.Serialization; // <--- TAMBAHKAN INI untuk JsonPropertyNameAttribute

namespace MyChatbotBackend.Models
{
    public class DocumentChunk
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // ID unik untuk setiap chunk
        public string Content { get; set; } = string.Empty; // Teks dari chunk dokumen
        public List<float> Embedding { get; set; } = new List<float>(); // Vektor embedding dari content
        // Properti lain bisa ditambahkan (misalnya, sumber dokumen, nomor halaman, dll.)
    }

    // Class untuk request ke Ollama Embeddings API
    public class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")] // Digunakan untuk serialisasi JSON
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")] // Digunakan untuk serialisasi JSON
        public string Prompt { get; set; } = string.Empty;
    }

    // Class untuk response dari Ollama Embeddings API
    public class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")] // Digunakan untuk serialisasi JSON
        public List<float> Embedding { get; set; } = new List<float>();
    }
}