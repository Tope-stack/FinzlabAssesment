namespace FinzlabAssesment.Models
{
    public class TransactionRequestModel
    {
    }


    public record TransactionPayload(
     string ExternalId,
     decimal Amount,
     string Currency,
     string Status,
     DateTime OccurredAt
    );
}
