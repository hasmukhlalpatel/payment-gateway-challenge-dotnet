using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Queries;
using PaymentGateway.Api.Services.Queries;
using PaymentGateway.Core.Exceptions;

namespace PaymentGateway.Api.Controllers;

[Route("api/Payments")] //kept old route for backward compatibility, but the new route with versioning is also added below.
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ControllerName("Payments")]
[Produces("application/json")]
[ApiController]
public class PaymentsQueryController(IPaymentQueryService queryService, ILogger<PaymentsQueryController> logger)
    : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDetailsResponse?>> GetPaymentAsync(Guid id)
    {
        logger.LogInformation("Received request for payment details with ID: {PaymentId}", id); // Log the receipt of the request for payment details, No PII data is logged here.
        try
        {
            var payment = await queryService.GetStatusAsync(id);
            return Ok(payment);
        }
        catch (PaymentException ex)
        {
            logger.LogWarning(ex, "Payment with ID {PaymentId} not found", id);
            return NotFound(new { Message = ex.Message });
        }   
    }
}