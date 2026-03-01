namespace PaymentGateway.Api.Models.Commands;

public record PostPaymentRequest
{
    public required string CardNumber { get; init; }
    public required int ExpiryMonth { get; init; }
    public required int ExpiryYear { get; init; }
    public required string Currency { get; init; }
    public required int Amount { get; init; }
    public required string Cvv { get; init; }
    //public required string IdempotencyKey { get; init; } // In a real implementation, this would be used to ensure idempotent processing of payment requests
}