using System;

using Microsoft.EntityFrameworkCore;
using AccountingERP.Models;

namespace AccountingERP.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TransactionRecord> Transactions => Set<TransactionRecord>();
}
