// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using Cratis.Arc.MongoDB;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Storage;
using KernelCore::Cratis.Chronicle.Namespaces;
using KernelCore::Cratis.Chronicle.Observation.Reactors.Kernel;
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

        // Re-initialize test-specific fields (e.g. Tcs, Reactor, Observers) on the current
        // fixture and update the shared MutableServiceRegistry so that DI resolves the new
        // instances for this test run.
        var capturingCollection = new CapturingServiceCollection();
        ConfigureServices(capturingCollection);
        if (capturingCollection.Count > 0)
        {
            var registry = Services.GetRequiredService<MutableServiceRegistry>();
            registry.Update(capturingCollection);
        }

        // 1. Signal disconnect — tears down all handler streams (cancels CancellationTokens),
        //    sets lifecycle.IsConnected = false, and assigns a fresh ConnectionId.
        var connection = Services.GetRequiredService<IChronicleConnection>();
        await connection.Lifecycle.Disconnected();

        // 2. Deactivate all grains so stale in-memory state (e.g. LastHandledEventSequenceNumber)
        //    is discarded. Now that streams are torn down, grains have no active subscriptions
        //    and can be deactivated.
        await DeactivateAllGrains();

        // 3. Remove all databases again. The previous test's DisposeAsync already dropped
        //    databases, but StateMachine.OnDeactivateAsync calls WriteStateAsync(), which
        //    auto-creates MongoDB databases with stale grain state. A second cleanup ensures
        //    grains start with a clean slate when they reactivate during RegisterAll.
        await ChronicleFixture.RemoveAllDatabases();

        // 3b. Re-bootstrap the kernel reactors for the system event store. The startup task
        //     that normally does this has been removed from the test silo (it deadlocks when
        //     PatchManager grain can't activate during silo startup). Since DB was wiped, the
        //     system Namespaces grain and ReactorsReactor are gone. Without this, events like
        //     EventStoreAdded/NamespaceAdded won't be processed and webhook/subscription
        //     definitions never get saved.
        var grainFactory = Services.GetRequiredService<IGrainFactory>();
        await grainFactory.GetGrain<INamespaces>(
            (string)KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName.System).EnsureDefault();
        var kernelReactors = Services.GetRequiredService<IReactors>();
        await kernelReactors.DiscoverAndRegister(
            KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName.System,
            KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default);

        // 3c. Drop all test event store databases a second time, preserving the system event
        //     store databases written by step 3b. Grain OnDeactivateAsync writes in step 2
        //     can re-create test databases after the drop in step 3, carrying stale event type
        //     schemas into the next test. By this point (after step 3b), all deactivation
        //     writes have had sufficient time to complete, so this second drop leaves a clean
        //     slate without destroying the kernel reactor state.
        await ChronicleFixture.RemoveAllDatabases(
            excludePrefixes: [(string)KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName.System]);

        // 3d. Reset the EventTypesStorage in-memory cache for the test event store.
        //     EventTypesStorage caches registered schemas in a ConcurrentBag<EventType> field
        //     that is never cleared by database drops — HasFor/GetFor check this cache first and
        //     can return stale schemas from a previous test's event types, causing
        //     EventTypeSchemaChanged when two test suites define the same event type name with
        //     different properties. Calling Populate() re-reads from MongoDB (now empty) and
        //     replaces the cache, ensuring the next RegisterAll starts with a clean schema state.
        var storage = Services.GetRequiredService<IStorage>();
        await storage.GetEventStore(Constants.EventStore).EventTypes.Populate();

        // 4. Re-discover artifacts from the current test fixture. Discover() creates new
        //    handler objects with fresh CancellationTokens (but does not register them yet).
        var eventStore = Services.GetRequiredService<IEventStore>();
        await eventStore.DiscoverAll();

        // 5. Reconnect — registers with ConnectedClients, re-creates keep-alive stream,
        //    then fires lifecycle.Connected() which triggers RegisterAll() via OnConnected.
        if (connection is ChronicleConnection chronicleConnection)
        {
            await chronicleConnection.Reconnect();
        }

        // Diagnostic: call RegisterAll directly so that any exception surfaces instead
        // of being swallowed by ConnectionLifecycle.Connected's catch block.
        // If lifecycle.Connected already succeeded, Register() is a no-op (_registered = true).
        // If it failed, this retries and surfaces the actual error.
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
        catch (OperationCanceledException)
        {
            // Timeout/cancellation while waiting for stabilization during teardown is safe to ignore.
        }
        catch (OrleansException)
        {
            // If the management grain is unavailable (e.g. silo is shutting down),
            // we can safely ignore the error — the grains will be deactivated anyway.
        }
    }
}
