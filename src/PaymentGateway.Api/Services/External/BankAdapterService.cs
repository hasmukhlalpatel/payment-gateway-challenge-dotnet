using System.Text.Json;

using PaymentGateway.Core.Exceptions;

namespace PaymentGateway.Api.Services.External;

public interface IBankAdapterService
{
    Task<BankAuthorizationResponse> AuthorizePaymentAsync(BankAuthorizationRequest request);
}

public class BankAdapterService(HttpClient httpClient, ILogger<BankAdapterService> logger) : IBankAdapterService
{
    public async Task<BankAuthorizationResponse> AuthorizePaymentAsync(BankAuthorizationRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("payments", request);

            if (!response.IsSuccessStatusCode)
                throw new BankException($"Bank returned {response.StatusCode}");

            var responseContent = await response.Content.ReadAsStringAsync();
            var bankResponse = JsonSerializer.Deserialize<BankAuthorizationResponse>(responseContent)!;

            return bankResponse;
        }
        //TODO : Handle Resilience specific exceptions like BrokenCircuitException, BulkheadRejectedException, OperationCanceledException, HttpRequestException, TimeoutException, TaskCanceledException etc.
        //and wrap them in BankException or return a specific result with false and indicating the failure reason.
        catch (HttpRequestException ex)
        {
            logger.LogError($"Bank communication failed: {ex.Message}");
            throw new BankException("Unable to communicate with acquiring bank", ex);
        }
    }
}
