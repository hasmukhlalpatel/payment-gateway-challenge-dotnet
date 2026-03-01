using PaymentGateway.Core.Enums;

namespace PaymentGateway.Api.Models.Domain
{
    public class Payment
    {
        // Guid based id can lead to data / Btree fragmentation in databases, which can impact performance. Use CreateVersion7 in Dotnet 10. 
        public required Guid Id { get; set; }
        // Guid is used as a unique identifier for the payment. It is not sequential and does not expose any information about the order or timing of payments, which enhances security. In production, consider using a more secure ID generation strategy if needed.
        // DisplayId is a user-friendly identifier that can be used in logs and responses without exposing sensitive information. It can be generated using a secure random generator or a hashing mechanism based on the actual ID.
        //public Guid DisplayId { get; set; }  // Use this for user-friendly display purposes, not for internal processing or database keys.
        public required string CardNumberMasked { get; set; }
        public required int ExpiryMonth { get; set; }
        public required int ExpiryYear { get; set; }
        public required int Amount { get; set; }
        public required string Currency { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required DateTime LastUpdatedAt { get; set; }
        public required PaymentStatus Status { get; set; }
        public string? AuthorizationCode { get; set; } = string.Empty;

    }
}
