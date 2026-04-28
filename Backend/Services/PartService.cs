using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Services
{
    /// <summary>
    /// Handles parts inventory CRUD with low-stock notification generation.
    /// </summary>
    public interface IPartService
    {
        Task<List<PartResponseDto>> GetAllAsync();
        Task<PartResponseDto?> GetByIdAsync(int id);
        Task<PartResponseDto> CreateAsync(PartDto dto);
        Task<PartResponseDto?> UpdateAsync(int id, PartDto dto);
        Task<bool> DeleteAsync(int id);
        Task CheckLowStockAsync(int partId, int adminUserId);
    }

    public class PartService : IPartService
    {
        private readonly ApplicationDbContext _context;

        public PartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PartResponseDto>> GetAllAsync()
        {
            return await _context.Parts
                .Include(p => p.Vendor)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => MapToResponseDto(p))
                .ToListAsync();
        }

        public async Task<PartResponseDto?> GetByIdAsync(int id)
        {
            var part = await _context.Parts
                .Include(p => p.Vendor)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            return part == null ? null : MapToResponseDto(part);
        }

        public async Task<PartResponseDto> CreateAsync(PartDto dto)
        {
            var part = new Part
            {
                Name = dto.Name,
                Description = dto.Description,
                Category = dto.Category,
                CostPrice = dto.CostPrice,
                SellingPrice = dto.SellingPrice,
                Stock = dto.Stock,
                MinStockLevel = dto.MinStockLevel,
                VendorId = dto.VendorId
            };

            _context.Parts.Add(part);
            await _context.SaveChangesAsync();

            // Reload with vendor navigation
            await _context.Entry(part).Reference(p => p.Vendor).LoadAsync();
            return MapToResponseDto(part);
        }

        public async Task<PartResponseDto?> UpdateAsync(int id, PartDto dto)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null || !part.IsActive) return null;

            part.Name = dto.Name;
            part.Description = dto.Description;
            part.Category = dto.Category;
            part.CostPrice = dto.CostPrice;
            part.SellingPrice = dto.SellingPrice;
            part.Stock = dto.Stock;
            part.MinStockLevel = dto.MinStockLevel;
            part.VendorId = dto.VendorId;
            part.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _context.Entry(part).Reference(p => p.Vendor).LoadAsync();
            return MapToResponseDto(part);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null) return false;

            // Soft delete
            part.IsActive = false;
            part.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Check if a part's stock is below the minimum level and create a notification for the admin.
        /// </summary>
        public async Task CheckLowStockAsync(int partId, int adminUserId)
        {
            var part = await _context.Parts.FindAsync(partId);
            if (part == null) return;

            if (part.Stock < part.MinStockLevel)
            {
                // Avoid duplicate notifications
                var exists = await _context.Notifications
                    .AnyAsync(n => n.Type == "LowStock"
                        && n.Message.Contains($"Part #{part.Id}")
                        && !n.IsRead);

                if (!exists)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = adminUserId,
                        Type = "LowStock",
                        Message = $"Low stock alert: \"{part.Name}\" (Part #{part.Id}) has only {part.Stock} units remaining (minimum: {part.MinStockLevel}).",
                    });
                    await _context.SaveChangesAsync();
                }
            }
        }

        private static PartResponseDto MapToResponseDto(Part p)
        {
            return new PartResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category,
                CostPrice = p.CostPrice,
                SellingPrice = p.SellingPrice,
                Stock = p.Stock,
                MinStockLevel = p.MinStockLevel,
                VendorId = p.VendorId,
                VendorName = p.Vendor?.CompanyName,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            };
        }
    }
}
