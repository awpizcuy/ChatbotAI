using Microsoft.AspNetCore.Mvc;
using MyChatbotBackend.Data;
using MyChatbotBackend.DTOs;
using MyChatbotBackend.Models;
using MyChatbotBackend.Services;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // <-- Tambahkan ini untuk DbUpdateException
using Microsoft.Extensions.Logging;   // <-- Tambahkan ini untuk logging

namespace MyChatbotBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IYourAIService _aiService;
        private readonly ILogger<FeedbackController> _logger; // <-- Tambahkan logger

        // Terima logger melalui Dependency Injection
        public FeedbackController(
            AppDbContext context, 
            IYourAIService aiService,
            ILogger<FeedbackController> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackDto feedbackDto)
        {
            _logger.LogInformation("Menerima request feedback dengan rating: {Rating}", feedbackDto.Rating);

            // --- Blok try-catch untuk menangkap error ---
            try
            {
                var feedback = new Feedback
                {
                    UserQuestion = feedbackDto.UserQuestion,
                    AIResponse = feedbackDto.AiResponse,
                    ConversationHistory = JsonSerializer.Serialize(feedbackDto.ConversationHistory),
                    Rating = feedbackDto.Rating,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Feedback berhasil disimpan ke database dengan ID: {FeedbackId}", feedback.Id);

                if (feedbackDto.Rating == "dislike")
                {
                    string improvedAnswer = await _aiService.GenerateImprovedAnswerAsync(feedbackDto.ConversationHistory, feedbackDto.AiResponse);
                    return Ok(new { newReply = improvedAnswer });
                }

                return Ok(new { message = "Feedback has been received successfully." });
            }
            catch (DbUpdateException ex)
            {
                // Ini akan menangkap error spesifik dari database
                _logger.LogError(ex, "DATABASE ERROR: {ErrorMessage}. Inner Exception: {InnerException}", ex.Message, ex.InnerException?.Message);
                return StatusCode(500, $"Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                // Ini akan menangkap error tak terduga lainnya
                _logger.LogError(ex, "UNEXPECTED ERROR di FeedbackController.");
                return StatusCode(500, "An unexpected internal server error occurred.");
            }
        }
    }
}
