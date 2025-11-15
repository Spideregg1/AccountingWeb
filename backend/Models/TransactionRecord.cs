using System;

public class TransactionRecord
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public TransactionType Type { get; set; }
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
