using Microsoft.AspNetCore.Mvc;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService) { _authService = authService; }

        /// <summary>Login with email and password.</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (result == null) return Unauthorized(ApiResponse<object>.Fail("Invalid email or password."));
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
        }

        /// <summary>Register a new customer account.</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var result = await _authService.RegisterCustomerAsync(dto);
            if (result == null) return BadRequest(ApiResponse<object>.Fail("Email already exists."));
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Registration successful."));
        }

        /// <summary>Change password for the logged-in user.</summary>
        [HttpPost("change-password")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _authService.ChangePasswordAsync(userId, dto);
            if (!result) return BadRequest(ApiResponse<object>.Fail("Current password is incorrect."));
            return Ok(ApiResponse<object>.Ok(new { }, "Password changed successfully."));
        }
    }
}
