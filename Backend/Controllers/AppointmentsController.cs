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
    public class AppointmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public AppointmentsController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAll()
        {
            var appointments = await _context.Appointments.Include(a => a.Customer).OrderByDescending(a => a.ScheduledDate)
                .Select(a => new AppointmentResponseDto { Id = a.Id, CustomerId = a.CustomerId, CustomerName = a.Customer.FullName, ScheduledDate = a.ScheduledDate, ServiceType = a.ServiceType, Status = a.Status, Notes = a.Notes, CreatedAt = a.CreatedAt })
                .ToListAsync();
            return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok(appointments));
        }

        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMy()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var appointments = await _context.Appointments.Include(a => a.Customer).Where(a => a.CustomerId == userId).OrderByDescending(a => a.ScheduledDate)
                .Select(a => new AppointmentResponseDto { Id = a.Id, CustomerId = a.CustomerId, CustomerName = a.Customer.FullName, ScheduledDate = a.ScheduledDate, ServiceType = a.ServiceType, Status = a.Status, Notes = a.Notes, CreatedAt = a.CreatedAt })
                .ToListAsync();
            return Ok(ApiResponse<List<AppointmentResponseDto>>.Ok(appointments));
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] AppointmentDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var appointment = new Appointment { CustomerId = userId, ScheduledDate = dto.ScheduledDate, ServiceType = dto.ServiceType, Notes = dto.Notes };
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<AppointmentResponseDto>.Ok(new AppointmentResponseDto { Id = appointment.Id, CustomerId = appointment.CustomerId, ScheduledDate = appointment.ScheduledDate, ServiceType = appointment.ServiceType, Status = appointment.Status, Notes = appointment.Notes, CreatedAt = appointment.CreatedAt }, "Appointment booked."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Update(int id, [FromBody] AppointmentUpdateDto dto)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound(ApiResponse<object>.Fail("Appointment not found."));
            if (dto.ScheduledDate.HasValue) appointment.ScheduledDate = dto.ScheduledDate.Value;
            if (!string.IsNullOrEmpty(dto.ServiceType)) appointment.ServiceType = dto.ServiceType;
            if (!string.IsNullOrEmpty(dto.Status)) appointment.Status = dto.Status;
            if (dto.Notes != null) appointment.Notes = dto.Notes;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { }, "Appointment updated."));
        }
    }
}
