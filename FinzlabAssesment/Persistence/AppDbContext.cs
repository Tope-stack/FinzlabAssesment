using FinzlabAssesment.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinzlabAssesment.Persistence
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<TransactionSummary> TransactionSummaries => Set<TransactionSummary>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Transaction>(e =>
            {
                e.HasKey(t => t.Id);
                // Unique constraint on ExternalId enforces idempotency at the DB level
                e.HasIndex(t => t.ExternalId).IsUnique();
                e.Property(t => t.Amount).HasColumnType("numeric(18,4)");
            });

            b.Entity<TransactionSummary>(e =>
            {
                e.HasKey(s => s.Id);
                e.HasOne(s => s.Transaction)
                 .WithOne(t => t.Summary)
                 .HasForeignKey<TransactionSummary>(s => s.TransactionId);
                e.Property(s => s.AmountUsd).HasColumnType("numeric(18,4)");
            });
        }
    }
}
