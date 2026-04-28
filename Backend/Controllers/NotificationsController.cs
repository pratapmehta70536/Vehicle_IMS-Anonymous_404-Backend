using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IReportService _reportService;

        public NotificationsController(ApplicationDbContext context, IEmailService emailService, IReportService reportService)
        {
            _context = context;
            _emailService = emailService;
            _reportService = reportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var notifications = await _context.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationResponseDto { Id = n.Id, Type = n.Type, Message = n.Message, IsRead = n.IsRead, CreatedAt = n.CreatedAt })
                .ToListAsync();
            return Ok(ApiResponse<List<NotificationResponseDto>>.Ok(notifications));
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound(ApiResponse<object>.Fail("Notification not found."));
            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { }, "Marked as read."));
        }

        [HttpPost("send-credit-reminders")]
        public async Task<IActionResult> SendCreditReminders()
        {
            var report = await _reportService.GetCustomerReportAsync();
            var sent = 0;
            foreach (var credit in report.OverdueCredits)
            {
                if (!string.IsNullOrEmpty(credit.Email))
                {
                    try
                    {
                        await _emailService.SendCreditReminderAsync(credit.Email, credit.CustomerName, credit.CreditAmount, credit.DaysOverdue);
                        sent++;
                    }
                    catch { /* Log and continue */ }
                }
            }
            return Ok(ApiResponse<object>.Ok(new { SentCount = sent, TotalOverdue = report.OverdueCredits.Count }, $"Sent {sent} credit reminder emails."));
        }
    }
}
