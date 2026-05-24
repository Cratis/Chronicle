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
        // instances for this test run. Clear first so that services from the previous test
        // (e.g. a different UserProjection variant) are not still discoverable by DiscoverAll.
        var registry = Services.GetRequiredService<MutableServiceRegistry>();
        registry.Clear();
        var capturingCollection = new CapturingServiceCollection();
        ConfigureServices(capturingCollection);
        if (capturingCollection.Count > 0)
        {
            registry.Update(capturingCollection);
        }

        // 1. Signal disconnect — tears down all handler streams (cancels CancellationTokens),
        //    sets lifecycle.IsConnected = false, and assigns a fresh ConnectionId.
        var connection = Services.GetRequiredService<IChronicleConnection>();
        await connection.Lifecycle.Disconnected();

        // 1b. Evict every cached IEventStore held by the shared IChronicleClient so the next
        //     Connect() does not fan out a RegisterAll for every event-store/namespace combination
        //     ever created by prior test classes. Without this, pairs of cached EventStores that
        //     share an event-store name (different namespaces — e.g. sourceTenantA/sourceTenantB)
        //     each invoke EventStores.Ensure(name) concurrently and race on the same kernel
        //     Reactors row.
        var chronicleClient = Services.GetRequiredService<IChronicleClient>();
        chronicleClient.EvictEventStores();

        // 2. Evict all cached projection pipelines. The ProjectionPipelineManager is a singleton
        //    service whose cache persists across test classes. Without this, a test that registers
        //    ProjectionX can leave a stale pipeline in the cache that is then reused — with the
        //    wrong schema or Sink state — when a later test registers a different projection that
        //    happens to resolve to the same pipeline key.
        Services.GetRequiredService<KernelCore::Cratis.Chronicle.Projections.Engine.Pipelines.IProjectionPipelineManager>().Clear();

        // 3. Wipe storage BEFORE deactivating grains. The order matters: wiping first truncates
        //    the OrleansReminders table so reminders cannot re-activate grains while we are
        //    deactivating, and it also truncates every Chronicle table so any in-flight grain
        //    that writes via OnDeactivateAsync (step 4) writes into a known-empty database. If
        //    we deactivated first, reminders would race the wipe by re-activating grains
        //    (notably EventStoreSubscriptionsManager, which schedules a 1-minute reminder),
        //    leaving their in-memory State populated from the previous test class — and that
        //    state would then be re-persisted on next WriteStateAsync, recreating rows the
        //    wipe was supposed to remove.
        //
        //    For out-of-process mode this resets the OOP container's backing store via gRPC
        //    (the OOP-container-side ICanPerformKernelStateReset wipes the kernel's files /
        //    tables AND invalidates its per-context migration cache atomically). For the
        //    in-process silo's SQL storage, the per-test fixture override invokes
        //    IDatabase.Wipe() on the test silo's IDatabase so the same atomic wipe + cache
        //    invalidation happens on this side too.
        await ChronicleFixture.RemoveAllDatabases();
        await WipeInProcessStorage();

        // 4. Deactivate all grains so stale in-memory state (e.g. State.Subscriptions held
        //    by EventStoreSubscriptionsManager) is discarded. With the OrleansReminders table
        //    truncated in step 3, no reminder can re-activate a grain we are trying to deactivate.
        await DeactivateAllGrains();

        // 4a. Re-wipe storage. Grain deactivation runs OnDeactivateAsync which writes any dirty
        //     in-memory state back to storage — so a [KeepAlive] grain that was active when the
        //     previous test finished (e.g. an Observer grain at NextEventSequenceNumber=2) will
        //     re-populate its row in the freshly-wiped database during deactivation. The next
        //     test then reads that stale row in Observer.Subscribe -> ReadStateAsync and
        //     silently filters out every appended event whose sequence number is below the
        //     stored NextEventSequenceNumber. Wiping again here catches anything written
        //     between the first wipe and the deactivation cycle.
        await ChronicleFixture.RemoveAllDatabases();
        await WipeInProcessStorage();

        // 4b. Evict the per-event-store storage cache so the next access reconstructs the
        //     namespace storage and its sinks from scratch. Sinks retain in-memory bookkeeping
        //     (bulk-mode flag, in-replay flag on the underlying collection helper, per-key
        //     state caches used during bulk writes) that the database wipe does not clear; a
        //     sink left mid-replay by a previous test would silently route the next test's
        //     writes into the temporary replay collection while the test reads from the real
        //     one. Doing this *after* DeactivateAllGrains is critical: while a subscriber grain
        //     is still active it holds a reference to its cached pipeline, which holds a
        //     reference to the old sink instance, and continues to write through the stale
        //     reference — evict before deactivation and the writes go to an orphaned sink that
        //     the new reader never sees.
        Services.GetRequiredService<IStorage>().Clear();

        // 3b. Re-bootstrap kernel reactors for the system event store and the test event store.
        //     The ChronicleServerStartupTask that normally does this has been removed from the
        //     test silo (it deadlocks during silo startup), and the previous test's DiscoverAll
        //     leaves nothing useful in the wiped database. Without re-registering here, events
        //     such as EventStoreAdded / NamespaceAdded never reach the kernel reactors and
        //     downstream webhook/subscription definitions never get saved.
        var grainFactory = Services.GetRequiredService<IGrainFactory>();
        var kernelReactors = Services.GetRequiredService<IReactors>();
        await grainFactory.GetGrain<INamespaces>(
            (string)KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName.System).EnsureDefault();
        await kernelReactors.DiscoverAndRegister(
            KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName.System,
            KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default);
        await grainFactory.GetGrain<INamespaces>(
            (string)(KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName)Constants.EventStore)
            .EnsureDefault();
        await kernelReactors.DiscoverAndRegister(
            (KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName)Constants.EventStore,
            KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName.Default);

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

    /// <summary>
    /// Gets an optional action to configure Chronicle storage on the in-process silo.
    /// Returns null to use the default MongoDB configuration.
    /// </summary>
    /// <param name="mongoServer">The MongoDB connection string from the fixture container.</param>
    /// <returns>An optional storage configurator action, or null for default MongoDB.</returns>
    protected virtual Action<KernelCore::Cratis.Chronicle.Configuration.IChronicleBuilder>? GetStorageConfigurator(string mongoServer) => null;

    /// <summary>
    /// Gets the default sink type identifier for projection registration.
    /// Returns default to keep the MongoDB default.
    /// </summary>
    /// <returns>The sink type identifier, or default to preserve existing behavior.</returns>
    protected virtual Sinks.SinkTypeId? GetDefaultSinkTypeId() => null;

    /// <summary>
    /// Gets additional host configuration key-value pairs to inject when the in-process silo
    /// uses a non-MongoDB storage backend. These are added to the host configuration before
    /// the silo options are bound, so <c>IOptions&lt;ChronicleOptions&gt;</c> picks up the correct
    /// storage type and connection string.
    /// Returns null (the default) when no extra configuration is needed.
    /// </summary>
    /// <param name="mongoServer">The MongoDB connection string from the fixture container.</param>
    /// <returns>An optional dictionary of configuration key-value pairs, or null for default MongoDB.</returns>
    protected virtual IReadOnlyDictionary<string, string?>? GetStorageHostConfiguration(string mongoServer) => null;

    /// <summary>
    /// Creates the in-process web application factory for the current test assembly.
    /// </summary>
    /// <returns>The web application factory instance for the discovered startup type.</returns>
    protected override IAsyncDisposable CreateWebApplicationFactory()
    {
        var startupType = TestAssembly!.ExportedTypes.FirstOrDefault(type => type.Name == "Startup");
        startupType ??= TestAssembly!.ExportedTypes.FirstOrDefault()!;
        var webApplicationFactoryType = typeof(ChronicleOrleansInProcessWebApplicationFactory<>).MakeGenericType(startupType!);
        var configureServices = ConfigureServices;
        var configureMongoDB = ConfigureMongoDB;
        var configureWebHostBuilder = ConfigureWebHostBuilder;

        // Determine storage configuration without reading the MongoDB port. No current override
        // of GetStorageConfigurator or GetStorageHostConfiguration uses the mongoServer parameter
        // for SQL modes, so passing an empty string is safe. The real port is only needed for the
        // MongoDB (default) path inside ChronicleOrleansInProcessWebApplicationFactory.
        var storageConfigurator = GetStorageConfigurator(string.Empty);
        var defaultSinkTypeId = GetDefaultSinkTypeId();
        var storageHostConfiguration = GetStorageHostConfiguration(string.Empty);
        return (Activator.CreateInstance(webApplicationFactoryType, [this, configureServices, configureMongoDB, configureWebHostBuilder, storageConfigurator, defaultSinkTypeId, storageHostConfiguration, ContentRoot]) as IAsyncDisposable)!;
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
    /// Wipes the in-process silo's SQL storage (files for SQLite, tables for PostgreSQL /
    /// Microsoft SQL Server) and atomically invalidates the silo's per-context migration cache.
    /// The default implementation is a no-op; SQL-backed fixtures override this to invoke
    /// <c>IDatabase.Wipe()</c> on the silo's <c>IDatabase</c> singleton. Doing the wipe and the
    /// cache invalidation in one operation prevents reminder-driven and deactivation-driven
    /// grain writes between the two steps from leaving the cache out of sync with the on-disk
    /// (or in-table) state.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected virtual Task WipeInProcessStorage() => Task.CompletedTask;

    /// <summary>
    /// Deactivates all Orleans grains so that stale in-memory state does not leak between tests.
    /// Waits until activation count stabilizes to ensure grains have fully deactivated before
    /// the next test starts registering artifacts against fresh grain activations.
    /// </summary>
    /// <returns>A task that completes when grain deactivation has stabilized.</returns>
    protected async Task DeactivateAllGrains()
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
