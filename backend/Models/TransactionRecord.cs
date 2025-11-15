namespace AccountingERP.Models;

using System.ComponentModel.DataAnnotations;

public class TransactionRecord
{
    public int Id { get; set; }

    [Required]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required]
    public TransactionType Type { get; set; }

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
