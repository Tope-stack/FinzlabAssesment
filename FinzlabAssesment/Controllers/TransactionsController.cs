using FinzlabAssesment.Entities;
using FinzlabAssesment.Models;
using FinzlabAssesment.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinzlabAssesment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TransactionsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("/webhooks/transactions")]
        public async Task<IActionResult> IngestTransaction([FromBody] TransactionPayload payload)
        {
            // 1. Idempotency — return 200 if we've already processed this event
            bool alreadyExists = await _db.Transactions
                .AnyAsync(t => t.ExternalId == payload.ExternalId);

            if (alreadyExists)
                return Ok(new { message = "Already processed", externalId = payload.ExternalId });

            // 2. Persist the raw transaction
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                ExternalId = payload.ExternalId,
                Currency = payload.Currency.ToUpperInvariant(),
                Amount = payload.Amount,
                Status = payload.Status,
                OccurredAt = payload.OccurredAt,
            };

            // 3. Derived computation — normalise to USD + risk classification
            decimal usdAmount = ConvertToUsd(payload.Amount, payload.Currency);

            var summary = new TransactionSummary
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                AmountUsd = usdAmount,
                RiskLevel = usdAmount > 10_000m ? "HIGH" : "LOW",
            };

            _db.Transactions.Add(transaction);
            _db.TransactionSummaries.Add(summary);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(IngestTransaction), new { id = transaction.Id }, new
            {
                transaction.Id,
                transaction.ExternalId,
                transaction.Currency,
                transaction.Amount,
                summary.AmountUsd,
                summary.RiskLevel,
            });
        }

        private static decimal ConvertToUsd(decimal amount, string currency) =>
            currency.ToUpperInvariant() switch
            {
                "USD" => amount,
                "EUR" => amount * 1.08m,
                "GBP" => amount * 1.27m,
                "NGN" => amount * 0.00065m,
                _ => amount
            };
    }
}

