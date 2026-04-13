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

        // After database cleanup the ConnectedClients grain has lost its state.
        // Re-register the client connection so observer grains can find it.
        var connection = Services.GetRequiredService<IChronicleConnection>();
        if (connection is ChronicleConnection chronicleConnection)
        {
            await chronicleConnection.Reconnect();
        }

        // Re-discover and re-register artifacts with the kernel so it knows about
        // this test's event types, observers, and projections.
        var eventStore = Services.GetRequiredService<IEventStore>();
        await eventStore.DiscoverAll();
        await eventStore.RegisterAll();

        // Observer grains survive across tests (ForceActivationCollection cannot
        // deactivate them because they hold active subscriptions). Their in-memory
        // LastHandledEventSequenceNumber is stale from the previous test. Replay
        // forces each observer to re-read its state from the (now clean) database
        // and re-process events from the beginning.
        await ReplayAllObservers(eventStore);
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
    /// Replays all registered projection, reactor, and reducer observers so their
    /// in-memory state is reset after a database cleanup.
    /// </summary>
    static async Task ReplayAllObservers(IEventStore eventStore)
    {
        var replayTasks = new List<Task>();

        foreach (var handler in eventStore.Projections.GetAllHandlers())
        {
            replayTasks.Add(eventStore.Projections.Replay(handler.Id));
        }

        foreach (var handler in eventStore.Reactors.GetAllHandlers())
        {
            replayTasks.Add(eventStore.Reactors.Replay(handler.Id));
        }

        foreach (var handler in eventStore.Reducers.GetAllHandlers())
        {
            replayTasks.Add(eventStore.Reducers.Replay(handler.Id));
        }

        await Task.WhenAll(replayTasks);
    }
}
