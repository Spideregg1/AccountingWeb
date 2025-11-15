using AccountingERP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AccountingERP.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateConverter = new ValueConverter<DateOnly, DateTime>(
            d => d.ToDateTime(TimeOnly.MinValue),
            d => DateOnly.FromDateTime(d));

        modelBuilder.Entity<TransactionRecord>()
            .Property(t => t.Date)
            .HasConversion(dateConverter)
            .HasColumnType("TEXT");

        modelBuilder.Entity<TransactionRecord>()
            .Property(t => t.Amount)
            .HasColumnType("NUMERIC");

        modelBuilder.Entity<TransactionRecord>()
            .Property(t => t.Category)
            .HasMaxLength(100);

        modelBuilder.Entity<TransactionRecord>()
            .Property(t => t.Note)
            .HasMaxLength(500);
    }
}
