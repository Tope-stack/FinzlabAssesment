namespace FinzlabAssesment.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public required string ExternalId { get; set; }  // idempotency key
        public required string Currency { get; set; }
        public decimal Amount { get; set; }
        public required string Status { get; set; }
        public DateTime OccurredAt { get; set; }
        public DateTime IngestedAt { get; set; } = DateTime.UtcNow;
        public TransactionSummary? Summary { get; set; }
    }

    public class TransactionSummary
    {
        public Guid Id { get; set; }
        public Guid TransactionId { get; set; }
        public decimal AmountUsd { get; set; }   // derived
        public required string RiskLevel { get; set; }  // HIGH | LOW — derived
        public Transaction? Transaction { get; set; }
    }
}
