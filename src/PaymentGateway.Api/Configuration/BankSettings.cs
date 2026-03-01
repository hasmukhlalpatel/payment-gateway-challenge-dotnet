namespace PaymentGateway.Api.Configuration;

public record BankSettings
{
    public required string BaseUrl { get; init; }
    public required int TimeoutInSeconds { get; init; }
}