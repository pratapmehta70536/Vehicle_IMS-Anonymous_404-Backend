using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Staff")]
    public class ChatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetChatUsers()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var users = await _context.Users
                .Where(u => u.IsActive && u.Id != userId && (u.Role == "Admin" || u.Role == "Staff"))
                .ToListAsync();

            var result = new List<ChatUserDto>();

            foreach (var u in users)
            {
                var lastMsg = await _context.ChatMessages
                    .Where(m => (m.SenderId == userId && m.ReceiverId == u.Id) || 
                                (m.SenderId == u.Id && m.ReceiverId == userId))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                var unreadCount = await _context.ChatMessages
                    .CountAsync(m => m.SenderId == u.Id && m.ReceiverId == userId && !m.IsRead);

                result.Add(new ChatUserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    LastMessageText = lastMsg?.MessageText,
                    LastMessageTime = lastMsg?.SentAt,
                    UnreadCount = unreadCount
                });
            }

            var sorted = result
                .OrderByDescending(r => r.LastMessageTime ?? DateTime.MinValue)
                .ThenBy(r => r.FullName)
                .ToList();

            return Ok(ApiResponse<List<ChatUserDto>>.Ok(sorted));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetTotalUnreadCount()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var unreadCount = await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
            return Ok(ApiResponse<int>.Ok(unreadCount));
        }

        [HttpGet("{otherUserId}")]
        public async Task<IActionResult> GetMessages(int otherUserId)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Mark incoming messages as read
            var unread = await _context.ChatMessages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();
            if (unread.Any())
            {
                unread.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) || 
                            (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .Select(m => new 
                {
                    m.Id,
                    m.SenderId,
                    SenderName = m.Sender.FullName,
                    m.ReceiverId,
                    ReceiverName = m.Receiver.FullName,
                    m.MessageText,
                    m.SentAt,
                    m.IsRead
                })
                .ToListAsync();
            return Ok(ApiResponse<object>.Ok(messages));
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendChatMessageDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (string.IsNullOrWhiteSpace(dto.MessageText))
            {
                return BadRequest(ApiResponse<object>.Fail("Message text cannot be empty."));
            }

            var receiverExists = await _context.Users.AnyAsync(u => u.Id == dto.ReceiverId && (u.Role == "Admin" || u.Role == "Staff"));
            if (!receiverExists)
            {
                return NotFound(ApiResponse<object>.Fail("Receiver not found or is not a staff/admin."));
            }

            var msg = new ChatMessage
            {
                SenderId = userId,
                ReceiverId = dto.ReceiverId,
                MessageText = dto.MessageText,
                SentAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(msg);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.Ok(new 
            {
                msg.Id,
                msg.SenderId,
                msg.ReceiverId,
                msg.MessageText,
                msg.SentAt
            }, "Message sent successfully."));
        }
    }

    public class SendChatMessageDto
    {
        public int ReceiverId { get; set; }
        public string MessageText { get; set; } = string.Empty;
    }

    public class ChatUserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? LastMessageText { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }
}
