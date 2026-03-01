using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Services.External;

public record BankAuthorizationRequest
{
    [JsonPropertyName("card_number")]
    public required string CardNumber { get; init; }

    [JsonPropertyName("expiry_date")]
    public required string ExpiryDate { get; init; }

    [JsonPropertyName("currency")]
    public required string Currency { get; init; }

    [JsonPropertyName("amount")]
    public required int Amount { get; init; }

    [JsonPropertyName("cvv")]
    public required string Cvv { get; init; }

    //MerchantId???
     //[JsonPropertyName("merchant_id")]
     //public required string CurrencyCode { get; init;
}

public record BankAuthorizationResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; init; }

    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; init; }
}
