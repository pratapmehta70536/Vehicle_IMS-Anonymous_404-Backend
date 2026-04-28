using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Staff")]
    public class SalesInvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IEmailService _emailService;

        public SalesInvoicesController(IInvoiceService invoiceService, IEmailService emailService)
        {
            _invoiceService = invoiceService;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var invoices = await _invoiceService.GetSalesInvoicesAsync();
            return Ok(ApiResponse<List<SalesInvoiceResponseDto>>.Ok(invoices));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _invoiceService.GetSalesInvoiceByIdAsync(id);
            if (invoice == null) return NotFound(ApiResponse<object>.Fail("Invoice not found."));
            return Ok(ApiResponse<SalesInvoiceResponseDto>.Ok(invoice));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SalesInvoiceCreateDto dto)
        {
            try
            {
                var staffId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var invoice = await _invoiceService.CreateSalesInvoiceAsync(dto, staffId);
                return Ok(ApiResponse<SalesInvoiceResponseDto>.Ok(invoice, invoice.LoyaltyApplied ? $"Invoice created. 10% loyalty discount applied! Saved Rs. {invoice.Discount:N2}" : "Invoice created."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPost("{id}/email")]
        public async Task<IActionResult> SendEmail(int id, [FromBody] SendInvoiceEmailDto dto)
        {
            var invoice = await _invoiceService.GetSalesInvoiceByIdAsync(id);
            if (invoice == null) return NotFound(ApiResponse<object>.Fail("Invoice not found."));

            var itemDetails = string.Join("", invoice.Items.Select(i => $"<p>{i.PartName} x{i.Quantity} = Rs. {i.TotalPrice:N2}</p>"));
            try
            {
                await _emailService.SendInvoiceEmailAsync(dto.ToEmail, invoice.CustomerName, invoice.Id, invoice.FinalAmount, itemDetails);
                return Ok(ApiResponse<object>.Ok(new { }, "Invoice emailed successfully."));
            }
            catch (Exception)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Failed to send email. Check SMTP configuration."));
            }
        }
    }
}
