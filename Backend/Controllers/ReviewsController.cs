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
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ReviewsController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _context.Reviews.Include(r => r.Customer).OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewResponseDto { Id = r.Id, CustomerId = r.CustomerId, CustomerName = r.Customer.FullName, Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt })
                .ToListAsync();
            return Ok(ApiResponse<List<ReviewResponseDto>>.Ok(reviews));
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] ReviewDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var review = new Review { CustomerId = userId, Rating = dto.Rating, Comment = dto.Comment };
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<ReviewResponseDto>.Ok(new ReviewResponseDto { Id = review.Id, CustomerId = review.CustomerId, Rating = review.Rating, Comment = review.Comment, CreatedAt = review.CreatedAt }, "Review submitted."));
        }
    }
}
