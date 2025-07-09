using Microsoft.EntityFrameworkCore;
using MyChatbotBackend.Models; // <-- INI BARIS KUNCI YANG MEMPERBAIKI ERROR

namespace MyChatbotBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Properti DbSet untuk setiap model/tabel
        public DbSet<UserQuestion> UserQuestions { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; } // <-- Baris ini sekarang mengenali 'Feedback'

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Konfigurasi bisa ditambahkan di sini jika perlu
            base.OnModelCreating(modelBuilder);
        }
    }
}