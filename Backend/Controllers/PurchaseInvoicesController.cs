using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class PurchaseInvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        public PurchaseInvoicesController(IInvoiceService invoiceService) { _invoiceService = invoiceService; }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var invoices = await _invoiceService.GetPurchaseInvoicesAsync();
            return Ok(ApiResponse<List<PurchaseInvoiceResponseDto>>.Ok(invoices));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _invoiceService.GetPurchaseInvoiceByIdAsync(id);
            if (invoice == null) return NotFound(ApiResponse<object>.Fail("Invoice not found."));
            return Ok(ApiResponse<PurchaseInvoiceResponseDto>.Ok(invoice));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PurchaseInvoiceCreateDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            var invoice = await _invoiceService.CreatePurchaseInvoiceAsync(dto, userId);
            return Ok(ApiResponse<PurchaseInvoiceResponseDto>.Ok(invoice, "Purchase invoice created. Stock updated."));
        }
    }
}
