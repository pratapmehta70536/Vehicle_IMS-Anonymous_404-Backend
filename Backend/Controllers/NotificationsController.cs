using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Services;
using Backend.Models;

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

            // Clear any stale notifications for parts that are no longer low stock
            var normalStockParts = await _context.Parts
                .Where(p => p.IsActive && p.Stock >= 10 && p.Stock >= p.MinStockLevel)
                .ToListAsync();

            var cleared = false;
            foreach (var part in normalStockParts)
            {
                var staleAlerts = await _context.Notifications
                    .Where(n => n.Type == "LowStock" && n.Message.Contains($"Part #{part.Id}"))
                    .ToListAsync();
                if (staleAlerts.Any())
                {
                    _context.Notifications.RemoveRange(staleAlerts);
                    cleared = true;
                }
            }
            if (cleared)
            {
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Safe to ignore if another thread/request cleared these notifications already
                }
            }

            // Dynamically scan for any existing low stock parts that don't have ANY notifications yet
            var lowStockParts = await _context.Parts
                .Where(p => p.IsActive && (p.Stock < 10 || p.Stock < p.MinStockLevel))
                .ToListAsync();

            if (lowStockParts.Any())
            {
                var generated = false;
                foreach (var part in lowStockParts)
                {
                    var exists = await _context.Notifications
                        .AnyAsync(n => n.Type == "LowStock"
                            && n.Message.Contains($"Part #{part.Id}"));

                    if (!exists)
                    {
                        var threshold = Math.Max(10, part.MinStockLevel);
                        _context.Notifications.Add(new Notification
                        {
                            UserId = userId,
                            Type = "LowStock",
                            Message = $"Low stock alert: \"{part.Name}\" (Part #{part.Id}) has only {part.Stock} units remaining (minimum: {threshold}).",
                        });
                        generated = true;
                    }
                }
                if (generated)
                {
                    await _context.SaveChangesAsync();
                }
            }

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

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var unread = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { Count = unread.Count }, $"Marked {unread.Count} notifications as read."));
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
