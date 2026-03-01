using System.Text.Json.Serialization;

using Asp.Versioning;

using FluentValidation;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

using PaymentGateway.Api.Configuration;
using PaymentGateway.Api.Middleware;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Services.Commands;
using PaymentGateway.Api.Services.External;
using PaymentGateway.Api.Services.Queries;
using PaymentGateway.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

builder.Services.Configure<BankSettings>(builder.Configuration.GetSection("BankSettings"));

builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddApiExplorer(options =>
    {
        // This helps resolve the {version:apiVersion} constraint in routes
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<PostPaymentRequestValidator>();
//builder.Services.AddTransient<IValidator<PostPaymentRequest>, PostPaymentRequestValidator>();


builder.Services
    .AddTransient<IPaymentCommandService, PaymentCommandService>()
    .AddTransient<IPaymentQueryService, PaymentQueryService>()
    .AddSingleton<IPaymentsRepository, PaymentsRepository>()
    .AddHttpClient<IBankAdapterService, BankAdapterService>((sp, client) =>
    {
        var settings = sp.GetRequiredService<IOptions<BankSettings>>().Value; 
        client.BaseAddress = new Uri(settings.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(settings.TimeoutInSeconds);// Slightly more than Resilience policy timeout
    })
    .AddStandardResilienceHandler(); // TODO: Implement Polly policies for fallback, bulkhead, retry, circuit breaker, timeout, etc. 

// Program.cs - Security Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("https://trusted-merchant.com")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// use Rate limiting to protect against abuse and DDoS attacks. In production, consider using a distributed rate limiter like Redis to handle multiple instances of the application.
// NOTE: No need this if we are using API Management or Azure Front Door which can handle rate limiting at the edge.
builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions
        .AddFixedWindowLimiter(policyName: "fixed", options =>
        {
            options.PermitLimit = 100;
            options.Window = TimeSpan.FromSeconds(60);
        });
});

//Encryption service - in production, consider using a secure key management solution like Azure Key Vault
//builder.Services.AddScoped<IEncryptionService, AzureKeyVaultEncryptionService>();

/*// Use Azure Key Vault to Get Secrets on startup or runtime
   var keyVaultUrl = new Uri($"https://{keyVaultName}.vault.azure.net/");
   var credential = new DefaultAzureCredential();
   builder.Configuration.AddAzureKeyVault(keyVaultUrl, credential);
 */

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app
    .UseMiddleware<LoggingMiddleware>()
    .UseMiddleware<ExceptionHandlingMiddleware>()
    .UseMiddleware<JwtValidationMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;