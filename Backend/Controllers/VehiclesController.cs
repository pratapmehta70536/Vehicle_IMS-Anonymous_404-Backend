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
    public class VehiclesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public VehiclesController(ApplicationDbContext context) { _context = context; }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var vehicles = await _context.Vehicles.Where(v => v.CustomerId == customerId)
                .Select(v => new VehicleResponseDto { Id = v.Id, CustomerId = v.CustomerId, VehicleNumber = v.VehicleNumber, Make = v.Make, Model = v.Model, Year = v.Year, Color = v.Color })
                .ToListAsync();
            return Ok(ApiResponse<List<VehicleResponseDto>>.Ok(vehicles));
        }

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyVehicles()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var vehicles = await _context.Vehicles.Where(v => v.CustomerId == userId)
                .Select(v => new VehicleResponseDto { Id = v.Id, CustomerId = v.CustomerId, VehicleNumber = v.VehicleNumber, Make = v.Make, Model = v.Model, Year = v.Year, Color = v.Color })
                .ToListAsync();
            return Ok(ApiResponse<List<VehicleResponseDto>>.Ok(vehicles));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VehicleDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var customerId = role == "Customer" ? userId : 0;
            if (customerId == 0) return BadRequest(ApiResponse<object>.Fail("Customer ID required."));

            var vehicle = new Vehicle { CustomerId = customerId, VehicleNumber = dto.VehicleNumber, Make = dto.Make, Model = dto.Model, Year = dto.Year, Color = dto.Color };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<VehicleResponseDto>.Ok(new VehicleResponseDto { Id = vehicle.Id, CustomerId = vehicle.CustomerId, VehicleNumber = vehicle.VehicleNumber, Make = vehicle.Make, Model = vehicle.Model, Year = vehicle.Year, Color = vehicle.Color }, "Vehicle added."));
        }

        [HttpPost("for-customer/{customerId}")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> CreateForCustomer(int customerId, [FromBody] VehicleDto dto)
        {
            var vehicle = new Vehicle { CustomerId = customerId, VehicleNumber = dto.VehicleNumber, Make = dto.Make, Model = dto.Model, Year = dto.Year, Color = dto.Color };
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<VehicleResponseDto>.Ok(new VehicleResponseDto { Id = vehicle.Id, CustomerId = vehicle.CustomerId, VehicleNumber = vehicle.VehicleNumber, Make = vehicle.Make, Model = vehicle.Model, Year = vehicle.Year, Color = vehicle.Color }, "Vehicle added."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VehicleDto dto)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound(ApiResponse<object>.Fail("Vehicle not found."));
            vehicle.VehicleNumber = dto.VehicleNumber; vehicle.Make = dto.Make; vehicle.Model = dto.Model; vehicle.Year = dto.Year; vehicle.Color = dto.Color;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<VehicleResponseDto>.Ok(new VehicleResponseDto { Id = vehicle.Id, CustomerId = vehicle.CustomerId, VehicleNumber = vehicle.VehicleNumber, Make = vehicle.Make, Model = vehicle.Model, Year = vehicle.Year, Color = vehicle.Color }, "Vehicle updated."));
        }
    }
}
