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
        await DisconnectClient();

        await base.DisposeAsync();
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeInitializeAsync()
    {
        if (_webApplicationFactory is null)
        {
            return;
        }

        // The silo is reused across tests. Point the artifacts provider at the current
        // fixture so that DiscoverAll picks up this test's event types, reactors, etc.
        if (Services.GetRequiredService<IClientArtifactsProvider>() is DelegatingClientArtifactsProvider delegating)
        {
            delegating.SetCurrent(this);
        }

        // Re-discover artifacts from the updated provider so the client-side registries
        // know about this test's types before any Establish/Because code runs.
        // This is a local operation — no server access is needed.
        var eventStore = Services.GetRequiredService<IEventStore>();
        await eventStore.DiscoverAll();

        // The client was disconnected in the previous test's DisposeAsync.
        // ConnectIfNotConnected() will lazily reconnect the first time the test
        // accesses IChronicleServicesAccessor.Services, which fires OnConnected
        // → RegisterAll → pushes discovered artifacts to the server.
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
    /// </summary>
    async Task DeactivateAllGrains()
    {
        try
        {
            var managementGrain = GrainFactory.GetGrain<IManagementGrain>(0);
            await managementGrain.ForceActivationCollection(TimeSpan.Zero);
        }
        catch
        {
            // If grain deactivation fails, the silo may be in a bad state — dispose the factory
            // so that the next test recreates it from scratch.
            await (_webApplicationFactory?.DisposeAsync() ?? ValueTask.CompletedTask);
            _webApplicationFactory = null;
        }
    }

    /// <summary>
    /// Disconnects the client so the next test triggers a fresh reconnection that
    /// re-registers all artifacts with the newly activated grains.
    /// </summary>
    async Task DisconnectClient()
    {
        try
        {
            var connection = Services.GetRequiredService<IChronicleConnection>();
            await connection.Lifecycle.Disconnected();
        }
        catch
        {
            // If disconnection fails, force-recreate the silo on the next test.
            await (_webApplicationFactory?.DisposeAsync() ?? ValueTask.CompletedTask);
            _webApplicationFactory = null;
        }
    }
}
