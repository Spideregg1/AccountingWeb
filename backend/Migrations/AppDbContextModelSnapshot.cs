using System;
using AccountingERP.Data;
using AccountingERP.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AccountingERP.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0");

            var dateConverter = new ValueConverter<DateOnly, DateTime>(
                v => v.ToDateTime(TimeOnly.MinValue),
                v => DateOnly.FromDateTime(v));

            modelBuilder.Entity("AccountingERP.Models.TransactionRecord", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<decimal>("Amount")
                    .HasColumnType("NUMERIC");

                b.Property<string>("Category")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("TEXT");

                b.Property<DateOnly>("Date")
                    .HasColumnType("TEXT")
                    .HasConversion(dateConverter);

                b.Property<string>("Note")
                    .HasMaxLength(500)
                    .HasColumnType("TEXT");

                b.Property<int>("Type")
                    .HasColumnType("INTEGER");

                b.HasKey("Id");

                b.ToTable("Transactions");
            });
#pragma warning restore 612, 618
        }
    }
}
