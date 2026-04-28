using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    /// <summary>
    /// Represents a line item in a sales invoice.
    /// </summary>
    public class SalesInvoiceItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SalesInvoiceId { get; set; }

        [Required]
        public int PartId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalPrice => Quantity * UnitPrice;

        // Navigation
        [ForeignKey("SalesInvoiceId")]
        public SalesInvoice SalesInvoice { get; set; } = null!;

        [ForeignKey("PartId")]
        public Part Part { get; set; } = null!;
    }
}
