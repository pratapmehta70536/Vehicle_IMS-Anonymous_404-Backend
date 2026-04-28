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
    public class CustomersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Services.IInvoiceService _invoiceService;

        public CustomersController(ApplicationDbContext context, Services.IInvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        [HttpGet]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _context.Users.Where(u => u.Role == "Customer" && u.IsActive)
                .Select(u => new UserResponseDto { Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role, Phone = u.Phone, Address = u.Address, IsActive = u.IsActive, CreatedAt = u.CreatedAt })
                .ToListAsync();
            return Ok(ApiResponse<List<UserResponseDto>>.Ok(customers));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var u = await _context.Users.Include(c => c.Vehicles).FirstOrDefaultAsync(u => u.Id == id && u.Role == "Customer");
            if (u == null) return NotFound(ApiResponse<object>.Fail("Customer not found."));
            var invoices = await _invoiceService.GetCustomerInvoicesAsync(id);
            return Ok(ApiResponse<object>.Ok(new
            {
                Customer = new UserResponseDto { Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role, Phone = u.Phone, Address = u.Address, IsActive = u.IsActive, CreatedAt = u.CreatedAt },
                Vehicles = u.Vehicles.Select(v => new VehicleResponseDto { Id = v.Id, CustomerId = v.CustomerId, VehicleNumber = v.VehicleNumber, Make = v.Make, Model = v.Model, Year = v.Year, Color = v.Color }),
                Invoices = invoices
            }));
        }

        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(ApiResponse<object>.Fail("Email already exists."));

            var customer = new User { FullName = dto.FullName, Email = dto.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password), Role = "Customer", Phone = dto.Phone, Address = dto.Address };
            _context.Users.Add(customer);
            await _context.SaveChangesAsync();

            if (dto.Vehicle != null)
            {
                _context.Vehicles.Add(new Vehicle { CustomerId = customer.Id, VehicleNumber = dto.Vehicle.VehicleNumber, Make = dto.Vehicle.Make, Model = dto.Vehicle.Model, Year = dto.Vehicle.Year, Color = dto.Vehicle.Color });
                await _context.SaveChangesAsync();
            }

            return Ok(ApiResponse<UserResponseDto>.Ok(new UserResponseDto { Id = customer.Id, FullName = customer.FullName, Email = customer.Email, Role = customer.Role, Phone = customer.Phone, Address = customer.Address, IsActive = customer.IsActive, CreatedAt = customer.CreatedAt }, "Customer registered."));
        }

        [HttpGet("search")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> Search([FromQuery] string? q)
        {
            if (string.IsNullOrWhiteSpace(q)) return Ok(ApiResponse<List<UserResponseDto>>.Ok(new List<UserResponseDto>()));

            var query = q.ToLower();
            var customers = await _context.Users
                .Include(u => u.Vehicles)
                .Where(u => u.Role == "Customer" && u.IsActive && (
                    u.FullName.ToLower().Contains(query) ||
                    u.Email.ToLower().Contains(query) ||
                    (u.Phone != null && u.Phone.Contains(query)) ||
                    u.Id.ToString() == query ||
                    u.Vehicles.Any(v => v.VehicleNumber.ToLower().Contains(query))
                ))
                .Select(u => new UserResponseDto { Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role, Phone = u.Phone, Address = u.Address, IsActive = u.IsActive, CreatedAt = u.CreatedAt })
                .Take(20)
                .ToListAsync();

            return Ok(ApiResponse<List<UserResponseDto>>.Ok(customers));
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateProfile([FromBody] CustomerUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(ApiResponse<object>.Fail("User not found."));

            if (!string.IsNullOrEmpty(dto.FullName)) user.FullName = dto.FullName;
            if (!string.IsNullOrEmpty(dto.Phone)) user.Phone = dto.Phone;
            if (!string.IsNullOrEmpty(dto.Address)) user.Address = dto.Address;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { }, "Profile updated."));
        }

        [HttpGet("my-invoices")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyInvoices()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var invoices = await _invoiceService.GetCustomerInvoicesAsync(userId);
            return Ok(ApiResponse<object>.Ok(invoices));
        }
    }
}
