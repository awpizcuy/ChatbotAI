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
using System.IO;
using System.Text.Json.Serialization;
using MyChatbotBackend.Data; // Untuk AppDbContext
using Microsoft.EntityFrameworkCore; // Untuk DbContext.Database.Migrate()
using System.Linq; // Untuk Enumerable.Repeat, Where, ToArray
using MyChatbotBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Tambahkan layanan CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowFrontendVueJs",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173") // Pastikan ini adalah port Vue.js Anda
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });

});

// Tambahkan layanan controllers ke kontainer DI
builder.Services.AddControllers();
// Baris ini memberitahu aplikasi cara membuat IYourAIService
builder.Services.AddScoped<IYourAIService, YourAIService>();

//Konfigurasi Database (sudah ada)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Tambahkan HttpClient untuk injeksi dependensi
builder.Services.AddHttpClient();

// --- TAMBAHKAN KONFIGURASI DATABASE SQLITE DI SINI ---
// Ini mendaftarkan AppDbContext ke sistem Dependency Injection
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
// --- AKHIR KONFIGURASI DATABASE SQLITE ---

// --- TAMBAHKAN Singleton untuk Vector Store RAG dengan penanganan error yang kuat ---
builder.Services.AddSingleton<List<DocumentChunk>>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var httpClient = sp.GetRequiredService<HttpClient>();
    var embeddingModelName = config["OllamaApi:EmbeddingModelName"] ?? "nomic-embed-text";
    var ollamaApiUrl = config["OllamaApi:Url"] ?? "http://localhost:11434";

    httpClient.BaseAddress = new Uri(ollamaApiUrl);

    List<DocumentChunk> documentChunks = new List<DocumentChunk>(); // Inisialisasi kosong
    string userManualContent = ""; // Variabel untuk menyimpan konten manual

    Console.WriteLine("Memulai inisialisasi basis pengetahuan (RAG) dari file TXT...");

    try // Try-catch utama untuk proses inisialisasi RAG
    {
        // --- LOKASI DAN PEMBACAAN FILE TXT ---
        string txtFilePath = Path.Combine(AppContext.BaseDirectory, "Content", "MODUL 1.txt");
        
        Console.WriteLine($"Mencari file TXT di: {txtFilePath}");

        if (File.Exists(txtFilePath))
        {
            try
            {
                userManualContent = File.ReadAllText(txtFilePath);
                Console.WriteLine($"Berhasil membaca {userManualContent.Length} karakter dari MODUL 1.txt.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Gagal membaca file TXT: {ex.Message}. Menggunakan konten fallback.");
                userManualContent = "ERROR: Gagal membaca file TXT utama. Pastikan format file benar atau tidak rusak."; // Fallback
            }
        }
        else
        {
            Console.WriteLine($"PERINGATAN: File TXT tidak ditemukan di: {txtFilePath}. Menggunakan konten fallback.");
            userManualContent = "PERINGATAN: File TXT tidak ditemukan di jalur yang diharapkan. Pastikan file MODUL 1.txt ada di folder Content."; // Fallback
        }

        // --- STRATEGI CHUNKING BARU DAN LEBIH ROBUST ---
        List<string> rawChunksList = new List<string>();
        int chunkSize = 500; // Ukuran setiap chunk dalam karakter
        int chunkOverlap = 100; // Jumlah karakter tumpang tindih antar chunk

        if (!string.IsNullOrWhiteSpace(userManualContent))
        {
            int currentPosition = 0;
            while (currentPosition < userManualContent.Length)
            {
                int length = Math.Min(chunkSize, userManualContent.Length - currentPosition);
                string chunk = userManualContent.Substring(currentPosition, length).Trim();

                if (!string.IsNullOrWhiteSpace(chunk))
                {
                    rawChunksList.Add(chunk);
                }
                currentPosition += chunkSize - chunkOverlap;
                if (currentPosition < 0) currentPosition = 0; // Pastikan tidak negatif
            }
        }
        var rawChunks = rawChunksList.ToArray(); // Konversi ke array
        // --- AKHIR STRATEGI CHUNKING BARU ---


        if (rawChunks.Length == 0)
        {
            Console.WriteLine("PERINGATAN: Dokumen kosong atau tidak menghasilkan chunk setelah pemecahan. Menggunakan chunk fallback.");
            documentChunks.Add(new DocumentChunk
            {
                Content = "Dokumen kosong atau tidak dapat diproses (hardcode fallback).",
                Embedding = new List<float>(Enumerable.Repeat(0f, 384)) // Contoh embedding dummy (ukuran 384 untuk nomic-embed-text)
            });
        }
        else
        {
            foreach (var chunkContent in rawChunks)
            {
                string processedChunkContent = chunkContent.Trim();
                // Batasi panjang chunk yang dikirim ke embedding API jika masih terlalu besar
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
                    var embeddingTask = httpClient.PostAsJsonAsync("api/embeddings", embeddingRequest);
                    var embeddingResponse = embeddingTask.Result; 
                    embeddingResponse.EnsureSuccessStatusCode();
                    var ollamaEmbedding = embeddingResponse.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>().Result;

                    documentChunks.Add(new DocumentChunk
                    {
                        Content = chunkContent.Trim(), // Simpan konten asli chunk
                        Embedding = ollamaEmbedding?.Embedding ?? new List<float>()
                    });
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"ERROR SAAT MENG-EMBED CHUNK (HttpRequestException): Gagal menghubungi Ollama embedding API. Pastikan '{embeddingModelName}' sudah di-pull dan Ollama berjalan di '{ollamaApiUrl}'. Pesan: {ex.Message}");
                }
                catch (System.Text.Json.JsonException ex)
                {
                    Console.WriteLine($"ERROR SAAT MENG-EMBED CHUNK (JsonException): Gagal mengurai respons embedding dari Ollama. Pesan: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR SAAT MENG-EMBED CHUNK (Umum): {ex.Message}");
                }
            }
        }
    }
    catch (Exception ex) // Tangkap semua exception yang mungkin terjadi di blok inisialisasi ini
    {
        Console.WriteLine($"FATAL ERROR SAAT INISIALISASI RAG: {ex.Message}");
        documentChunks = new List<DocumentChunk>(); 
        Console.WriteLine("Inisialisasi RAG gagal. Vector store akan kosong. Chatbot akan menjawab di luar jangkauan.");
    }

    Console.WriteLine($"Basis pengetahuan diinisialisasi. Jumlah chunk: {documentChunks.Count}");
    return documentChunks;
});
// --- AKHIR TAMBAHAN Singleton ---

// Konfigurasi Swagger/OpenAPI untuk dokumentasi API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Tambahkan Migrasi Database saat Startup (Opsional, tapi Direkomendasikan untuk Pengembangan) ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // dbContext.Database.Migrate(); // Tetap dikomentari karena ini yang menyebabkan error CS1501
        Console.WriteLine("Migrasi database tidak otomatis dijalankan. Pastikan Anda menjalankan 'dotnet ef database update' secara manual.");
        Console.WriteLine("Database berhasil terhubung atau sudah up to date (melalui pengecekan koneksi).");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR SAAT MENGAKSES DATABASE UNTUK MIGRASI: {ex.Message}");
        Console.WriteLine("Database mungkin tidak dibuat atau diperbarui.");
    }
}
// --- Akhir Migrasi Database Otomatis ---

// Konfigurasi HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontendVueJs");

app.UseAuthorization();

app.MapControllers();

app.Run();