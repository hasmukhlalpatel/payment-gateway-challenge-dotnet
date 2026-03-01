namespace PaymentGateway.Api.Configuration
{
    public record AppSettings
    {
        public required BankSettings BankSettings { get; init; }
    }
}
