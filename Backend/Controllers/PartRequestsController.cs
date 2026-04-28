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
    [Authorize]
    public class PartRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public PartRequestsController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var requests = await _context.PartRequests.Include(pr => pr.Customer).OrderByDescending(pr => pr.CreatedAt)
                .Select(pr => new PartRequestResponseDto { Id = pr.Id, CustomerId = pr.CustomerId, CustomerName = pr.Customer.FullName, PartName = pr.PartName, Description = pr.Description, Status = pr.Status, CreatedAt = pr.CreatedAt })
                .ToListAsync();
            return Ok(ApiResponse<List<PartRequestResponseDto>>.Ok(requests));
        }

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMy()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var requests = await _context.PartRequests.Where(pr => pr.CustomerId == userId).OrderByDescending(pr => pr.CreatedAt)
                .Select(pr => new PartRequestResponseDto { Id = pr.Id, CustomerId = pr.CustomerId, CustomerName = "", PartName = pr.PartName, Description = pr.Description, Status = pr.Status, CreatedAt = pr.CreatedAt })
                .ToListAsync();
            return Ok(ApiResponse<List<PartRequestResponseDto>>.Ok(requests));
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] PartRequestDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var request = new PartRequest { CustomerId = userId, PartName = dto.PartName, Description = dto.Description };
            _context.PartRequests.Add(request);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<PartRequestResponseDto>.Ok(new PartRequestResponseDto { Id = request.Id, CustomerId = request.CustomerId, PartName = request.PartName, Description = request.Description, Status = request.Status, CreatedAt = request.CreatedAt }, "Part request submitted."));
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
        {
            var request = await _context.PartRequests.FindAsync(id);
            if (request == null) return NotFound(ApiResponse<object>.Fail("Request not found."));
            request.Status = status;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { }, $"Status updated to {status}."));
        }
    }
}
