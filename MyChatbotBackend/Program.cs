using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http; // Tambahkan ini jika belum ada

var builder = WebApplication.CreateBuilder(args);

// Tambahkan layanan CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowFrontendVueJs",
                      policy =>
                      {
                          // ***** PENTING: GANTI DENGAN URL SEBENARNYA DARI APLIKASI VUE.JS ANDA *****
                          // Berdasarkan screenshot Anda, Vue.js berjalan di http://localhost:5173
                          policy.WithOrigins("http://localhost:5173") // <-- PASTIKAN INI SESUAI
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });
});

// Tambahkan layanan controllers ke kontainer DI
builder.Services.AddControllers();

// Tambahkan HttpClient untuk injeksi dependensi
builder.Services.AddHttpClient();

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

app.UseHttpsRedirection(); // Menggunakan HTTPS redirection

// ***** PENTING: Gunakan CORS policy yang telah dibuat. *****
// Ini harus dipanggil SEBELUM app.UseAuthorization()
app.UseCors("AllowFrontendVueJs"); // Pastikan nama policy sesuai dengan yang Anda definisikan di atas

app.UseAuthorization(); // Menggunakan otorisasi

// Memetakan controller ke endpoint HTTP
app.MapControllers();

// Jalankan aplikasi web
app.Run();