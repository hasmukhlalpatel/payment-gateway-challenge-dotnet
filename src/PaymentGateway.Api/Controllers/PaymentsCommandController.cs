using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models.Commands;
using PaymentGateway.Api.Services.Commands;
using PaymentGateway.Core.Enums;

namespace PaymentGateway.Api.Controllers;

[Route("api/Payments")] //kept old route for backward compatibility, but the new route with versioning is also added below.
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[ControllerName("Payments")]
[Produces("application/json")]
[ApiController]
public class PaymentsCommandController(IValidator<PostPaymentRequest> validator,
    IPaymentCommandService commandService, ILogger<PaymentsCommandController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync([FromBody] PostPaymentRequest request)
    {
        logger.LogInformation("Received payment request"); // Log the receipt of the payment request only, No PII data is logged here.
        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .Select(e => new
                {
                    e.PropertyName, e.ErrorMessage
                });

            var errorResponse = new 
            {
                Message = "Validation failed",
                Status = PaymentStatus.Rejected,
                Errors = errors
            };
            return BadRequest(errorResponse);
        }

        var successResponse = await commandService.ExecutePaymentAsync(request);

        return Created($"api/Payments/{successResponse.Id}", successResponse);
    }
}
