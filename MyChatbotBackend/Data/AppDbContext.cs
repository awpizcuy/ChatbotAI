// MyChatbotBackend/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using MyChatbotBackend.Models; // Pastikan ini di-include untuk mengakses UserQuestion

namespace MyChatbotBackend.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor yang menerima DbContextOptions, ini akan digunakan untuk konfigurasi database
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Properti DbSet untuk setiap model yang ingin Anda simpan di database
        // Ini akan menjadi representasi tabel 'UserQuestions' di database SQLite Anda
        public DbSet<UserQuestion> UserQuestions { get; set; }

        // Konfigurasi tambahan untuk model jika diperlukan (misalnya indeks unik, nama tabel kustom)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Contoh: Mengatur QuestionText agar tidak null
            modelBuilder.Entity<UserQuestion>()
                .Property(q => q.QuestionText)
                .IsRequired();

            // --- TAMBAHKAN KONFIGURASI UNTUK AiResponse DI SINI ---
            modelBuilder.Entity<UserQuestion>()
                .Property(q => q.AiResponse)
                .IsRequired();

            // Contoh: Menentukan nama tabel secara eksplisit (opsional, defaultnya UserQuestions)
            modelBuilder.Entity<UserQuestion>().ToTable("UserQuestions");
        }
    }
}