// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a fixture for Orleans integration tests.
/// </summary>
/// <typeparam name="TChronicleFixture">The type of the chronicle fixture.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ChronicleOrleansFixture{T}"/> class.
/// </remarks>
/// <param name="chronicleFixture"><see cref="ChronicleInProcessFixture"/> to use.</param>
public class ChronicleOrleansFixture<TChronicleFixture>(TChronicleFixture chronicleFixture) : ChronicleClientFixture<TChronicleFixture>(chronicleFixture)
    where TChronicleFixture : IChronicleFixture
{
    /// <inheritdoc/>
    public override async Task DisposeAsync()
    {
        await DeactivateAllGrains();
        await base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeInitializeAsync()
    {
        // The silo is reused across tests. Point the artifacts provider at the current
        // fixture so that DiscoverAll picks up this test's event types, reactors, etc.
        DelegatingClientArtifactsProvider.Instance?.SetCurrent(this);

        // For the very first test the factory hasn't been created yet — InitializeFixture
        // will create it and the DI-registered IEventStore will do the initial discovery.
        if (_webApplicationFactory is null)
        {
            return;
        }

        // After grain deactivation, the ConnectedClients grain has lost its state.
        // Re-register the client connection so observer grains can find it.
        var connection = Services.GetRequiredService<IChronicleConnection>();
        if (connection is ChronicleConnection chronicleConnection)
        {
            await chronicleConnection.Reconnect();
        }

        // Re-discover and re-register artifacts with the (now fresh) grains so they
        // know about this test's event types, observers, and projections.
        var eventStore = Services.GetRequiredService<IEventStore>();
        await eventStore.DiscoverAll();
        await eventStore.RegisterAll();
    }

    /// <inheritdoc/>
    protected override IAsyncDisposable CreateWebApplicationFactory()
    {
        var startupType = TestAssembly!.ExportedTypes.FirstOrDefault(type => type.Name == "Startup");
        startupType ??= TestAssembly!.ExportedTypes.FirstOrDefault()!;
        var webApplicationFactoryType = typeof(ChronicleOrleansInProcessWebApplicationFactory<>).MakeGenericType(startupType!);
        var configureServices = ConfigureServices;
        var configureMongoDB = ConfigureMongoDB;
        var configureWebHostBuilder = ConfigureWebHostBuilder;
        return (Activator.CreateInstance(webApplicationFactoryType, [this, configureServices, configureMongoDB, configureWebHostBuilder, ContentRoot]) as IAsyncDisposable)!;
    }

    /// <summary>
    /// Overridable method to configure services.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to configure.</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Method to configure MongoDB options.
    /// </summary>
    /// <param name="mongoDBBuilder"><see cref="IMongoDBBuilder"/> to configure.</param>
    protected virtual void ConfigureMongoDB(IMongoDBBuilder mongoDBBuilder)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
    }

    /// <summary>
    /// Deactivates all Orleans grains so that stale in-memory state does not leak between tests.
    /// Waits until activation count stabilizes to ensure grains have fully deactivated before
    /// the next test starts registering artifacts against fresh grain activations.
    /// </summary>
    async Task DeactivateAllGrains()
    {
        try
        {
            var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);
            await managementGrain.ForceActivationCollection(TimeSpan.Zero);

            // ForceActivationCollection only schedules deactivation; the actual deactivation
            // happens asynchronously. Poll until the activation count stabilizes so that
            // grains are not still mid-deactivation when the next test registers artifacts.
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var stableReadCount = 0;
            var previousCount = -1;

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                var currentCount = await managementGrain.GetTotalActivationCount();
                if (currentCount == previousCount)
                {
                    if (++stableReadCount >= 3)
                    {
                        break;
                    }
                }
                else
                {
                    stableReadCount = 0;
                    previousCount = currentCount;
                }

                await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
        catch
        {
            // If grain deactivation fails, the silo may be in a bad state — dispose the factory
            // so that the next test recreates it from scratch.
            await (_webApplicationFactory?.DisposeAsync() ?? ValueTask.CompletedTask);
            _webApplicationFactory = null;
        }
    }
}
