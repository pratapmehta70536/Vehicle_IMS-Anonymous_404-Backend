using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        public ReportsController(IReportService reportService) { _reportService = reportService; }

        [HttpGet("financial")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetFinancialReport([FromQuery] string period = "monthly", [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var report = await _reportService.GetFinancialReportAsync(period, startDate, endDate);
            return Ok(ApiResponse<FinancialReportDto>.Ok(report));
        }

        [HttpGet("customers")]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> GetCustomerReport()
        {
            var report = await _reportService.GetCustomerReportAsync();
            return Ok(ApiResponse<CustomerReportDto>.Ok(report));
        }
    }
}
