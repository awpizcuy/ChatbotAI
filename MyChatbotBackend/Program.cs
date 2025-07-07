// MyChatbotBackend/Program.cs

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyChatbotBackend.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization; // Tambahkan ini jika dibutuhkan, tapi tidak wajib untuk kode ini
using System.IO;

// using PdfPig; // <--- BARIS INI TIDAK LAGI DIPERLUKAN DAN HARUS DIHAPUS/DIKOMENTARI
// using System.IO; // <--- BARIS INI TIDAK LAGI DIPERLUKAN DAN HARUS DIHAPUS/DIKOMENTARI

var builder = WebApplication.CreateBuilder(args);

// Tambahkan layanan CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowFrontendVueJs",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173") // <-- PASTIKSAN INI '5173'
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });
});

// Tambahkan layanan controllers ke kontainer DI
builder.Services.AddControllers();

// Tambahkan HttpClient untuk injeksi dependensi
builder.Services.AddHttpClient();

// Tambahkan Singleton untuk Vector Store RAG (akan memuat teks hardcode saat startup)
builder.Services.AddSingleton<List<DocumentChunk>>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var httpClient = sp.GetRequiredService<HttpClient>();
    var embeddingModelName = config["OllamaApi:EmbeddingModelName"] ?? "nomic-embed-text";
    var ollamaApiUrl = config["OllamaApi:Url"] ?? "http://localhost:11434";

    httpClient.BaseAddress = new Uri(ollamaApiUrl);

    Console.WriteLine("Memulai inisialisasi basis pengetahuan (RAG) dari file TXT...");

    // --- KONTEN MANUAL YANG DI-HARDCODE ---
    // HANYA BAGIAN INI YANG DIGUNAKAN SEBAGAI SUMBER userManualContent
    string txtFilePath = Path.Combine(AppContext.BaseDirectory, "Content", "MODUL 1.txt");
    string userManualContent = "";
    // --- AKHIR KONTEN MANUAL YANG DI-HARDCODE ---

    // Memecah dokumen menjadi chunks (Contoh sederhana: pecah berdasarkan dua baris baru)
     if (!File.Exists(txtFilePath))
    {
        Console.WriteLine($"ERROR: File TXT tidak ditemukan di: {txtFilePath}. Mohon pastikan jalur file benar.");
        Console.WriteLine("Menggunakan konten fallback karena file TXT tidak ditemukan.");
        // Konten fallback jika file tidak ditemukan
        userManualContent = "Ini adalah konten fallback karena file user manual tidak ditemukan atau tidak dapat dibaca. Mohon tempatkan file MODUL 1.txt di folder Content dalam direktori proyek backend Anda.";
    }
    else
    {
        try
        {
            // Membaca semua teks dari file TXT
            userManualContent = File.ReadAllText(txtFilePath);
            Console.WriteLine($"Berhasil membaca {userManualContent.Length} karakter dari MODUL 1.txt.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Gagal membaca file TXT: {ex.Message}");
            Console.WriteLine("Menggunakan konten fallback karena gagal membaca file TXT.");
            userManualContent = "Ini adalah konten fallback karena terjadi error saat mencoba membaca file TXT user manual. Pastikan file tidak rusak atau memiliki izin yang benar.";
        }
    }
    // --- AKHIR LOKASI DAN PEMBACAAN FILE TXT ---

    // Memecah dokumen menjadi chunks (Contoh sederhana: pecah berdasarkan dua baris baru)
    var rawChunks = userManualContent.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
    var documentChunks = new List<DocumentChunk>();

    foreach (var chunkContent in rawChunks)
    {
        if (string.IsNullOrWhiteSpace(chunkContent)) continue;

        string processedChunkContent = chunkContent.Trim();
        if (processedChunkContent.Length > 1024)
        {
            processedChunkContent = processedChunkContent.Substring(0, 1024);
        }

        Console.WriteLine($"Meng-embed chunk: {processedChunkContent.Substring(0, Math.Min(processedChunkContent.Length, 50))}...");

        var embeddingRequest = new OllamaEmbeddingRequest
        {
            Model = embeddingModelName,
            Prompt = processedChunkContent
        };

        try
        {
            var embeddingResponse = httpClient.PostAsJsonAsync("api/embeddings", embeddingRequest).Result;
            embeddingResponse.EnsureSuccessStatusCode();
            var ollamaEmbedding = embeddingResponse.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>().Result;

            documentChunks.Add(new DocumentChunk
            {
                Content = chunkContent.Trim(),
                Embedding = ollamaEmbedding?.Embedding ?? new List<float>()
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR saat meng-embed chunk: {ex.Message}");
            // Lanjutkan, tapi log error. Chunk ini mungkin akan dilewati.
        }
    }
    Console.WriteLine($"Basis pengetahuan diinisialisasi. Jumlah chunk: {documentChunks.Count}");
    return documentChunks;
});

// Konfigurasi Swagger/OpenAPI untuk dokumentasi API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Konfigurasi HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Gunakan CORS policy yang telah dibuat. PENTING: Ini harus dipanggil SEBELUM app.UseAuthorization()
app.UseCors("AllowFrontendVueJs");

app.UseAuthorization();

app.MapControllers();

app.Run();