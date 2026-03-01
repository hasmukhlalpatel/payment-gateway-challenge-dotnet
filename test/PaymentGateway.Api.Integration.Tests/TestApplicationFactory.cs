using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Services.External;

namespace PaymentGateway.Api.Integration.Tests;

using System;
using System.IO;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

public class TestApplicationFactory : WebApplicationFactory<Program>
{
    internal PaymentsRepository PaymentsRepository = new();

    private IContainer _mountebankContainer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Start a Testcontainers container for bbyars/mountebank using the same
        // settings as docker-compose.yml (ports, command, volume) so integration
        // tests have a controllable bank simulator.
        StartMountebankContainerIfNeeded();

        builder.ConfigureServices(services =>
        {
            // Example: override dependencies for testing
            // services.Remove(...);
            // services.AddSingleton<IMyService, FakeMyService>();
            services.AddSingleton<IPaymentsRepository>(PaymentsRepository);
            // Replace external bank adapter with a fake implementation for integration tests
            //services.AddSingleton<IBankAdapterService, FakeBankAdapterService>();
        });
    }

    private void StartMountebankContainerIfNeeded()
    {
        if (_mountebankContainer != null)
        {
            return;
        }

        // Compose values taken from repository docker-compose.yml:
        // image: bbyars/mountebank:2.8.1
        // ports: "2525:2525", "8080:8080"
        // command: --configfile /imposters/bank_simulator.ejs --allowInjection
        // volumes: ./imposters:/imposters

        // Resolve host path for './imposters' relative to the current working directory.
        string hostImpostersPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "imposters"));
        var builder = new ContainerBuilder()
            .WithImage("bbyars/mountebank:2.8.1")
            .WithName("bank_simulator_test")
            .WithPortBinding(2525, 2525)
            .WithPortBinding(8080, 8080)
            .WithCommand("--configfile", "/imposters/bank_simulator.ejs", "--allowInjection");

        if (Directory.Exists(hostImpostersPath))
        {
            builder = builder.WithBindMount(hostImpostersPath, "/imposters");
        }

        _mountebankContainer = builder.Build();

        // Start synchronously so tests can rely on the simulator being available.
        _mountebankContainer.StartAsync().GetAwaiter().GetResult();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_mountebankContainer != null)
            {
                try
                {
                    _mountebankContainer.StopAsync().GetAwaiter().GetResult();
                    _mountebankContainer.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                catch
                {
                    // best-effort stop/cleanup in test environment
                }
                finally
                {
                    _mountebankContainer = null;
                }
            }
        }

        base.Dispose(disposing);
    }

    private class FakeBankAdapterService : IBankAdapterService
    {
        public Task<BankAuthorizationResponse> AuthorizePaymentAsync(BankAuthorizationRequest request)
        {
            return Task.FromResult(new BankAuthorizationResponse
            {
                Authorized = true,
                AuthorizationCode = "TESTAUTH"
            });
        }
    }
}
