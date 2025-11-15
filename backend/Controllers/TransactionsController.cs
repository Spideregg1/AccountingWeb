using AccountingERP.Data;
using AccountingERP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingERP.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController(AppDbContext db) : ControllerBase
{
    private readonly AppDbContext _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TransactionRecord>>> GetTransactions(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] TransactionType? type,
        [FromQuery] string? category,
        [FromQuery] string? search)
    {
        var query = _db.Transactions.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(t => t.Date >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.Date <= endDate.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryFilter = $"%{category.Trim()}%";
            query = query.Where(t => EF.Functions.Like(t.Category, categoryFilter));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFilter = $"%{search.Trim()}%";
            query = query.Where(t =>
                EF.Functions.Like(t.Category, searchFilter) ||
                (t.Note != null && EF.Functions.Like(t.Note, searchFilter)));
        }

        var items = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionRecord>> GetTransaction(int id)
    {
        var transaction = await _db.Transactions.FindAsync(id);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionRecord>> CreateTransaction([FromBody] TransactionRecord transaction)
    {
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTransaction(int id, [FromBody] TransactionRecord updated)
    {
        if (id != updated.Id)
        {
            return BadRequest("Id mismatch");
        }

        var existing = await _db.Transactions.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Date = updated.Date;
        existing.Type = updated.Type;
        existing.Category = updated.Category;
        existing.Amount = updated.Amount;
        existing.Note = updated.Note;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var transaction = await _db.Transactions.FindAsync(id);
        if (transaction is null)
        {
            return NotFound();
        }

        _db.Transactions.Remove(transaction);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<IEnumerable<DailyTransactionSummary>>> GetCalendarSummary([FromQuery] int year, [FromQuery] int month)
    {
        if (year < 1 || month < 1 || month > 12)
        {
            return BadRequest("Invalid year or month value");
        }

        var firstDay = new DateOnly(year, month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var summaries = await _db.Transactions
            .Where(t => t.Date >= firstDay && t.Date <= lastDay)
            .GroupBy(t => t.Date)
            .Select(g => new DailyTransactionSummary(
                g.Key,
                g.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
                g.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                g.Count()))
            .OrderBy(s => s.Date)
            .ToListAsync();

        return summaries;
    }
}

public sealed record DailyTransactionSummary(DateOnly Date, decimal TotalIncome, decimal TotalExpense, int Count)
{
    public decimal NetAmount => TotalIncome - TotalExpense;
}
