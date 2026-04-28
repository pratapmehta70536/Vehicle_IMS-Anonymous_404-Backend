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
    [Authorize(Roles = "Admin")]
    public class StaffController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public StaffController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var staff = await _context.Users.Where(u => u.Role == "Staff")
                .Select(u => new UserResponseDto { Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role, Phone = u.Phone, Address = u.Address, IsActive = u.IsActive, CreatedAt = u.CreatedAt })
                .ToListAsync();
            return Ok(ApiResponse<List<UserResponseDto>>.Ok(staff));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var u = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Staff");
            if (u == null) return NotFound(ApiResponse<object>.Fail("Staff not found."));
            return Ok(ApiResponse<UserResponseDto>.Ok(new UserResponseDto { Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role, Phone = u.Phone, Address = u.Address, IsActive = u.IsActive, CreatedAt = u.CreatedAt }));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StaffDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(ApiResponse<object>.Fail("Email already exists."));

            var user = new User { FullName = dto.FullName, Email = dto.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), Role = "Staff", Phone = dto.Phone, Address = dto.Address };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<UserResponseDto>.Ok(new UserResponseDto { Id = user.Id, FullName = user.FullName, Email = user.Email, Role = user.Role, Phone = user.Phone, Address = user.Address, IsActive = user.IsActive, CreatedAt = user.CreatedAt }, "Staff created."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] StaffUpdateDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Staff");
            if (user == null) return NotFound(ApiResponse<object>.Fail("Staff not found."));

            user.FullName = dto.FullName;
            if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;
            user.Phone = dto.Phone;
            user.Address = dto.Address;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<UserResponseDto>.Ok(new UserResponseDto { Id = user.Id, FullName = user.FullName, Email = user.Email, Role = user.Role, Phone = user.Phone, Address = user.Address, IsActive = user.IsActive, CreatedAt = user.CreatedAt }, "Staff updated."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Staff");
            if (user == null) return NotFound(ApiResponse<object>.Fail("Staff not found."));
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { }, "Staff deactivated."));
        }

        /// <summary>Admin resets a staff member's password without needing the old one.</summary>
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == "Staff");
            if (user == null) return NotFound(ApiResponse<object>.Fail("Staff not found."));

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { }, "Password reset successfully."));
        }
    }
}
