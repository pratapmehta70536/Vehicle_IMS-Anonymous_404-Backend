using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PartsController : ControllerBase
    {
        private readonly IPartService _partService;
        public PartsController(IPartService partService) { _partService = partService; }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var parts = await _partService.GetAllAsync();
            return Ok(ApiResponse<List<PartResponseDto>>.Ok(parts));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var part = await _partService.GetByIdAsync(id);
            if (part == null) return NotFound(ApiResponse<object>.Fail("Part not found."));
            return Ok(ApiResponse<PartResponseDto>.Ok(part));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] PartDto dto)
        {
            var part = await _partService.CreateAsync(dto);
            return Ok(ApiResponse<PartResponseDto>.Ok(part, "Part created."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] PartDto dto)
        {
            var part = await _partService.UpdateAsync(id, dto);
            if (part == null) return NotFound(ApiResponse<object>.Fail("Part not found."));
            return Ok(ApiResponse<PartResponseDto>.Ok(part, "Part updated."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _partService.DeleteAsync(id);
            if (!result) return NotFound(ApiResponse<object>.Fail("Part not found."));
            return Ok(ApiResponse<object>.Ok(new { }, "Part deleted."));
        }
    }
}
