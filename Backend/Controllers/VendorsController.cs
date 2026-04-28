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
    public class VendorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public VendorsController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vendors = await _context.Vendors.Where(v => v.IsActive).OrderBy(v => v.CompanyName)
                .Select(v => new VendorResponseDto { Id = v.Id, CompanyName = v.CompanyName, ContactName = v.ContactName, Email = v.Email, Phone = v.Phone, Address = v.Address, IsActive = v.IsActive, CreatedAt = v.CreatedAt })
                .ToListAsync();
            return Ok(ApiResponse<List<VendorResponseDto>>.Ok(vendors));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var v = await _context.Vendors.FindAsync(id);
            if (v == null || !v.IsActive) return NotFound(ApiResponse<object>.Fail("Vendor not found."));
            return Ok(ApiResponse<VendorResponseDto>.Ok(new VendorResponseDto { Id = v.Id, CompanyName = v.CompanyName, ContactName = v.ContactName, Email = v.Email, Phone = v.Phone, Address = v.Address, IsActive = v.IsActive, CreatedAt = v.CreatedAt }));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VendorDto dto)
        {
            var vendor = new Vendor { CompanyName = dto.CompanyName, ContactName = dto.ContactName, Email = dto.Email, Phone = dto.Phone, Address = dto.Address };
            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<VendorResponseDto>.Ok(new VendorResponseDto { Id = vendor.Id, CompanyName = vendor.CompanyName, ContactName = vendor.ContactName, Email = vendor.Email, Phone = vendor.Phone, Address = vendor.Address, IsActive = vendor.IsActive, CreatedAt = vendor.CreatedAt }, "Vendor created."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VendorDto dto)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound(ApiResponse<object>.Fail("Vendor not found."));
            vendor.CompanyName = dto.CompanyName; vendor.ContactName = dto.ContactName; vendor.Email = dto.Email; vendor.Phone = dto.Phone; vendor.Address = dto.Address;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<VendorResponseDto>.Ok(new VendorResponseDto { Id = vendor.Id, CompanyName = vendor.CompanyName, ContactName = vendor.ContactName, Email = vendor.Email, Phone = vendor.Phone, Address = vendor.Address, IsActive = vendor.IsActive, CreatedAt = vendor.CreatedAt }, "Vendor updated."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound(ApiResponse<object>.Fail("Vendor not found."));
            vendor.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { }, "Vendor deleted."));
        }
    }
}
