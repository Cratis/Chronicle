// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Captures;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Generations.Admission;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Patching;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Releases;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Setup.Authentication;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Orleans.Hosting;

/// <summary>
/// Represents a startup task for Chronicle.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for storing data.</param>
/// <param name="eventTypes"><see cref="IEventTypes"/> for managing kernel event types.</param>
/// <param name="reactors"><see cref="IReactors"/> for managing kernel reactors.</param>
/// <param name="patternCapture"><see cref="IPatternCapture"/> for observing events for behavior pattern mining.</param>
/// <param name="projectionsServiceClient"><see cref="IProjectionsServiceClient"/> for registering projections with local silos.</param>
/// <param name="kernelProjections"><see cref="IKernelProjections"/> for registering the projections the kernel declares for itself.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="authenticationService"><see cref="IAuthenticationService"/> for managing authentication.</param>
/// <param name="storageErrorClassifier"><see cref="IStorageErrorClassifier"/> for recognizing the active storage provider's own transient failures.</param>
/// <param name="localSiloDetails">The caller's silo for patch-manager handover.</param>
/// <param name="clusterReleases">The published releases for startup diagnostics.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">Optional <see cref="TimeProvider"/> the retry backoff waits on; defaults to <see cref="TimeProvider.System"/>.</param>
/// <param name="generationFloorAdmission">Optional read-only generation admission; defaults to the real storage-backed admission.</param>
/// <param name="startupWorkEscalation">Optional <see cref="IStartupWorkEscalation"/> told when background startup work keeps failing; defaults to structured logging.</param>
/// <remarks>
/// Every step in <see cref="Execute"/> is a grain call that can land on a sibling silo, and every
/// silo runs this task as it starts. A sibling that has not stabilized yet answers none of them, and
/// an unhandled timeout here terminates the whole host - which is how one slow silo took both
/// replicas into a reinforcing crash loop: each restart took a new address the other silo then could
/// not reach either, while consuming applications saw nothing but timeouts. Retrying membership
/// instability as a class is what breaks that loop, and is why this does not special-case whichever
/// call site was unlucky enough to be the one that raced. See <see cref="SiblingSiloInstability"/>
/// for what counts as instability and what is still allowed to fail on the first attempt.
/// <para>
/// Several steps also provision SQL Server/PostgreSQL databases and schemas the first time a store
/// or namespace is touched, and several kernels can race that provisioning exactly as they can race
/// a sibling silo. A command timeout or connection failure from that contention is the storage
/// provider's own transient failure, not cluster instability, so it is recognized through the
/// registered <see cref="IStorageErrorClassifier"/> instead of being folded into
/// <see cref="SiblingSiloInstability"/>, which stays provider-agnostic.
/// </para>
/// <para>
/// Only the prerequisites for safe requests are awaited here. Work that scales with the backlog - activating
/// observers, resuming jobs, starting webhooks, captures and event store subscriptions - runs in a background
/// pass after them (see <see cref="RecoverableStartupWork"/>), so it neither delays readiness nor stops the host
/// when it is slow or failing.
/// </para>
/// </remarks>
internal sealed class ChronicleServerStartupTask(
    IStorage storage,
    IEventTypes eventTypes,
    IReactors reactors,
    IPatternCapture patternCapture,
    IProjectionsServiceClient projectionsServiceClient,
    IKernelProjections kernelProjections,
    IGrainFactory grainFactory,
    IAuthenticationService authenticationService,
    IStorageErrorClassifier storageErrorClassifier,
    ILocalSiloDetails localSiloDetails,
    IClusterReleases clusterReleases,
    ILogger<ChronicleServerStartupTask> logger,
    TimeProvider? timeProvider = null,
    IGenerationFloorAdmission? generationFloorAdmission = null,
    IStartupWorkEscalation? startupWorkEscalation = null) : ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    /// <summary>
    /// The exact Orleans placement-failure prefix for this grain and interface.
    /// </summary>
    internal const string PatchManagerPlacementFailure = "No active nodes are compatible with grain patchmanager and interface Cratis.Chronicle.Patching.IPatchManager version ";

    /// <summary>
    /// How many times a step is attempted before its failure is believed.
    /// </summary>
    /// <remarks>
    /// Each attempt already absorbs Orleans' own 30 second response timeout, and the backoff adds
    /// half a minute on top, so the budget spans several minutes of cluster formation - longer than
    /// membership normally takes to settle. A step still failing after that is not waiting on a
    /// sibling silo. Required steps fail the host; optional pattern capture is logged and reconciled later.
    /// </remarks>
    const int MaxAttempts = 5;

    static readonly TimeSpan _initialRetryDelay = TimeSpan.FromSeconds(2);

    static readonly TimeSpan _patchManagerRelocationDelay = TimeSpan.FromSeconds(1);

    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly RecoverableStartupWork _recoverableStartupWork = new(
        timeProvider ?? TimeProvider.System,
        startupWorkEscalation ?? new LoggingStartupWorkEscalation(logger),
        logger);

    readonly CancellationTokenSource _stopping = new();
    Task _recoverableWork = Task.CompletedTask;

    /// <summary>
    /// Gets the background pass, which completes once every recoverable item has succeeded or the silo stopped.
    /// </summary>
    internal Task RecoverableWork => _recoverableWork;

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle)
    {
        lifecycle.Subscribe(
            nameof(ChronicleServerStartupTask),
            ServiceLifecycleStage.Active,
            Execute,
            OnStop);
    }

    /// <inheritdoc/>
    public void Dispose() => _stopping.Dispose();

    /// <summary>
    /// Recognizes only the patch manager's no-compatible-node response during manifest convergence.
    /// </summary>
    /// <param name="exception">The Orleans placement failure.</param>
    /// <returns>Whether the bounded patch-manager relocation loop should retry.</returns>
    internal static bool IsPatchManagerAwaitingManifest(OrleansException exception) =>
        exception.Message.StartsWith(PatchManagerPlacementFailure, StringComparison.Ordinal);

    /// <summary>
    /// Cancels the background pass when the silo stops and waits for it to wind down.
    /// </summary>
    /// <param name="cancellationToken">Token that abandons the wait when the silo cannot wait any longer.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task OnStop(CancellationToken cancellationToken)
    {
        await _stopping.CancelAsync();
        try
        {
            await _recoverableWork.WaitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is OperationCanceledException or TimeoutException)
        {
            // The pass observes the same cancellation and never outlives the silo's own shutdown.
        }
    }

    /// <summary>
    /// Determines whether an observer's activation is required for startup.
    /// </summary>
    /// <param name="identifier">The observer identifier.</param>
    /// <returns>Whether the observer must activate successfully.</returns>
    /// <remarks>
    /// Pattern capture is optional. Its subscription/recovery path begins the alert lifecycle, so neither
    /// rehydration nor alert bootstrap may turn capture activation into a required startup step.
    /// </remarks>
    static bool IsRequiredObserver(ObserverId identifier) => identifier != PatternCapture.ObserverIdentifier;

    async Task Execute(CancellationToken cancellationToken)
    {
        await (generationFloorAdmission ?? new GenerationFloorAdmission(storage)).Admit(cancellationToken);

        Task Step(string step, Func<Task> action) => RunStep(step, action, cancellationToken);

        // Populate the local CLR map before any grain can deserialize system events.
        await Step("DiscoverAndRegisterSystemEventTypes", () => eventTypes.DiscoverAndRegister(EventStoreName.System));

        // Apply patches before other grains start
        var patchManager = grainFactory.GetGrain<IPatchManager>(0);
        await Step(nameof(IPatchManager.ApplyPatches), () => ApplyPatches(patchManager, cancellationToken));

        await Step("EnsureDefaultSystemNamespace", grainFactory.GetGrain<INamespaces>(EventStoreName.System).EnsureDefault);

        // Register reactors for the system event store first, so ReactorsReactor can process EventStoreAdded/NamespaceAdded events
        await Step("DiscoverAndRegisterSystemReactors", () => reactors.DiscoverAndRegister(EventStoreName.System, EventStoreNamespaceName.Default, cancellationToken));

        var recoverableStores = new List<EventStoreName>();
        var recoverableNamespaces = new ConcurrentBag<(EventStoreName EventStore, EventStoreNamespaceName Namespace)>();
        var allEventStores = await storage.GetEventStores().WaitAsync(cancellationToken);
        foreach (var eventStore in allEventStores)
        {
            // A registry entry without a usable name cannot be addressed as a grain; starting it would stop the silo
            // from starting at all, so it is reported and left for an operator instead.
            if (eventStore is null || string.IsNullOrWhiteSpace(eventStore.Value))
            {
                logger.SkippingEventStoreWithoutAName();
                continue;
            }

            if (eventStore != EventStoreName.System)
            {
                await Step("DiscoverAndRegisterEventTypes", () => eventTypes.DiscoverAndRegister(eventStore));
            }
            try
            {
                await Step("ReconcileLifecycleOccurrences", grainFactory.GetGrain<ILifecycleOccurrences>(eventStore.Value).Ensure);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.FailedReconcilingLifecycleOccurrences(exception, eventStore.Value);
            }
            await RehydrateOccurrences(eventStore, null, cancellationToken);
            var namespaces = grainFactory.GetGrain<INamespaces>(eventStore);
            await Step("EnsureDefaultNamespace", namespaces.EnsureDefault);

            recoverableStores.Add(eventStore);

            var readModelsManager = grainFactory.GetGrain<IReadModelsManager>(eventStore);
            await Step("EnsureReadModels", readModelsManager.Ensure);

            var projectionsManager = grainFactory.GetGrain<IProjectionsManager>(eventStore);
            await Step("EnsureProjections", projectionsManager.Ensure);

            // Before the persisted definitions are registered below, so the kernel's own projections are part of
            // the set that pass registers rather than a second registration racing it.
            await Step("DiscoverAndRegisterKernelProjections", () => kernelProjections.DiscoverAndRegister(eventStore));

            var projectionDefinitions = await projectionsManager.GetProjectionDefinitions().WaitAsync(cancellationToken);
            await Step("RegisterPersistedProjectionDefinitions", () => RegisterPersistedProjectionDefinitions(eventStore, projectionDefinitions));

            // Bound the namespace fan-out so starting a store with many tenants does not flood the cluster.
            await Parallel.ForEachAsync(await namespaces.GetAll().WaitAsync(cancellationToken), new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = cancellationToken }, async (namespaceName, token) =>
            {
                var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(namespaceName);
                var hasData = await namespaceStorage.HasData().WaitAsync(token);

                // Capture must be ready for the first append even in a namespace created before this restart.
                // There will be no new NamespaceAdded broadcast for an already registered namespace.
                try
                {
                    await Step("SubscribePatternCapture", () => patternCapture.Subscribe(eventStore, namespaceName));
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                {
                    // Analytics must not fail the Active lifecycle stage. The event log reconciles a missed
                    // subscription on its timer, including namespaces whose first append happens after startup.
                    logger.FailedSubscribingPatternCapture(exception, eventStore, namespaceName);
                }

                await RehydrateOccurrences(eventStore, namespaceName, token);

                if (!hasData)
                {
                    // There are no persisted jobs, application observers or event sequences to rehydrate.
                    return;
                }

                await Step("DiscoverAndRegisterReactors", () => reactors.DiscoverAndRegister(eventStore, namespaceName, cancellationToken));

                await Step("RehydrateEventSequences", grainFactory.GetEventSequences(eventStore, namespaceName).Rehydrate);
                recoverableNamespaces.Add((eventStore, namespaceName));
            });
        }

        await EnsureAdministrator(cancellationToken);
        await Step(nameof(IAuthenticationService.EnsureBootstrapClients), authenticationService.EnsureBootstrapClients);
#if DEVELOPMENT
        await Step(nameof(IAuthenticationService.EnsureDefaultClientCredentials), authenticationService.EnsureDefaultClientCredentials);
#endif

        // Everything a request needs is now in place. The backlog-proportional work continues without gating readiness.
        var stopping = _stopping.Token;
        _recoverableWork = Task.Run(() => RunRecoverableWork([.. recoverableStores], [.. recoverableNamespaces], stopping), CancellationToken.None);
    }

    async Task RunRecoverableWork(
        IReadOnlyList<EventStoreName> eventStores,
        IReadOnlyList<(EventStoreName EventStore, EventStoreNamespaceName Namespace)> namespaces,
        CancellationToken cancellationToken)
    {
        var started = _timeProvider.GetTimestamp();
        try
        {
            await Task.WhenAll(eventStores.Select(_ => RecoverEventStore(_, cancellationToken))
                .Concat(namespaces.Select(_ => RecoverNamespace(_.EventStore, _.Namespace, cancellationToken))));
            if (!cancellationToken.IsCancellationRequested)
            {
                logger.RecoverableStartupWorkCompleted(_timeProvider.GetElapsedTime(started));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The silo is stopping.
        }
        catch (Exception exception)
        {
            logger.RecoverableStartupWorkFaulted(exception);
        }
    }

    Task RecoverEventStore(EventStoreName eventStore, CancellationToken cancellationToken)
    {
        Task Run(string kind, Func<Task> action) => _recoverableStartupWork.Run(new(kind, eventStore.Value), _ => action(), cancellationToken);

        return Task.WhenAll(
            Run("EnsureEventStoreSubscriptions", grainFactory.GetGrain<IEventStoreSubscriptionsManager>(eventStore).Ensure),
            Run("EnsureWebhooks", grainFactory.GetGrain<IWebhooks>(eventStore).Ensure),
            Run("EnsureCaptures", grainFactory.GetGrain<ICapturesManager>(eventStore).Ensure));
    }

    async Task RecoverNamespace(EventStoreName eventStore, EventStoreNamespaceName namespaceName, CancellationToken cancellationToken)
    {
        var scope = $"{eventStore.Value}/{namespaceName.Value}";

        // Jobs first, as before: observers reconcile the partition holds of jobs that are already rehydrated.
        await _recoverableStartupWork.Run(new("RehydrateJobs", scope), _ => grainFactory.GetJobsManager(eventStore, namespaceName).Rehydrate(), cancellationToken);
        await Task.WhenAll(
            ActivateObservers("RehydrateObservers", scope, () => ResolveReducerAndReactorObserverKeys(eventStore, namespaceName), cancellationToken),
            ActivateObservers("BootstrapObserverAlerts", scope, () => ResolveObserverKeysNeedingAlertBootstrap(eventStore, namespaceName), cancellationToken));
    }

    async Task ActivateObservers(string kind, string scope, Func<Task<IReadOnlyList<ObserverKey>>> resolve, CancellationToken cancellationToken)
    {
        IReadOnlyList<ObserverKey> observerKeys = [];
        await _recoverableStartupWork.Run(new($"Resolve{kind}", scope), async _ => observerKeys = await resolve(), cancellationToken);

        // One independent, retried item per observer key: a slow or failing observer never holds up the others,
        // and the same key is never activated by two overlapping attempts from this pass.
        await Task.WhenAll(observerKeys.Select(key => _recoverableStartupWork.Run(
            new("ActivateObserver", key.ToString()),
            _ => grainFactory.GetGrain<IObserver>(key).Ensure(),
            cancellationToken)));
    }

    async Task ApplyPatches(IPatchManager patchManager, CancellationToken cancellationToken)
    {
        RequestContext.Set(PatchManagerRelocation.CallerSiloKey, localSiloDetails.SiloAddress);
        try
        {
            var delay = _patchManagerRelocationDelay;
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (await patchManager.ApplyPatchesFor(Cratis.Chronicle.Sys.KernelVersion.Current) == PatchOutcome.Completed) return;
                }
                catch (OrleansException exception) when (IsPatchManagerAwaitingManifest(exception))
                {
                    // A v2 placement can arrive before the new silo's manifest has converged.
                }

                // The older activation deactivates once idle; back off exponentially, since under load its
                // deactivation can take longer than a short fixed wait allows.
                if (attempt < MaxAttempts - 1)
                {
                    await Task.Delay(delay, _timeProvider, cancellationToken);
                    delay += delay;
                }
            }
            logger.PatchManagerCouldNotRelocate(Cratis.Chronicle.Sys.KernelVersion.Current.ToString(), clusterReleases.Summarize().ToString());
            throw new InvalidOperationException("Patch manager could not relocate to a kernel at least as new as the caller.");
        }
        finally
        {
            RequestContext.Remove(PatchManagerRelocation.CallerSiloKey);
        }
    }

    async Task EnsureAdministrator(CancellationToken cancellationToken)
    {
        await RunStep(nameof(IAuthenticationService.EnsureDefaultAdminUser), authenticationService.EnsureDefaultAdminUser, cancellationToken);
        await RunStep(nameof(Cratis.Chronicle.Security.IAdministratorRoster.EnsureAdministratorPrivilege), () => grainFactory.GetGrain<Cratis.Chronicle.Security.IAdministratorRoster>(Cratis.Chronicle.Concepts.Security.AdministratorRoster.Key).EnsureAdministratorPrivilege(), cancellationToken);
    }

    /// <summary>
    /// Runs one startup step, riding out a sibling silo that has not stabilized yet.
    /// </summary>
    /// <param name="step">The name of the step, for the operator reading the log.</param>
    /// <param name="action">The work to perform.</param>
    /// <param name="cancellationToken">Token for cancelling startup and retry backoff.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Only the failures <see cref="SiblingSiloInstability.IsTransient"/> or the registered
    /// <see cref="IStorageErrorClassifier"/> recognize are retried. Anything else - and anything
    /// still failing once the budget is spent - propagates, so a genuine defect still fails the
    /// host on startup rather than being started around.
    /// </remarks>
    async Task RunStep(string step, Func<Task> action, CancellationToken cancellationToken)
    {
        var delay = _initialRetryDelay;
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await action().WaitAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (IsTransient(exception) && attempt < MaxAttempts)
            {
                logger.RetryingStartupStep(exception, step, attempt, MaxAttempts, delay);
                await Task.Delay(delay, _timeProvider, cancellationToken);
                delay += delay;
            }
            catch (Exception exception) when (IsTransient(exception))
            {
                logger.StartupStepExhaustedRetries(exception, step, MaxAttempts);
                throw;
            }
            catch (Exception exception) when (IncompatibleSiloRelease.IsLikely(exception))
            {
                logger.StartupStepReachedIncompatibleSilo(exception, step, Cratis.Chronicle.Sys.KernelVersion.Current.ToString(), clusterReleases.Summarize().ToString());
                throw;
            }
        }
    }

    bool IsTransient(Exception exception) => SiblingSiloInstability.IsTransient(exception) || storageErrorClassifier.IsTransient(exception);

    /// <summary>
    /// Registers the persisted projection definitions for an event store with the local silo.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the definitions belong to.</param>
    /// <param name="projectionDefinitions">The persisted <see cref="ProjectionDefinition"/> instances to register.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// This call is the most expensive one in <see cref="Execute"/>, because registering fans out a
    /// subscribe across every observer in the store rather than doing the work of a single grain -
    /// a production store was observed fanning out to 782 of them inside one call. Since the whole
    /// fan-out has to answer within Orleans' one response timeout, the budget effectively shrinks as
    /// a store grows, and the call is therefore the likeliest in the task to exceed it. It must go
    /// through <see cref="RunStep"/> for the same reason every other call here does: an unhandled
    /// timeout terminates the host, and a host that cannot start cannot start on the next attempt
    /// either, so the store stays down until someone intervenes.
    /// </remarks>
    async Task RegisterPersistedProjectionDefinitions(EventStoreName eventStore, IEnumerable<ProjectionDefinition> projectionDefinitions)
    {
        var result = await projectionsServiceClient.Register(eventStore, projectionDefinitions);
        if (result.TryGetError(out var error))
        {
            foreach (var (identifier, failure) in error.Failures)
            {
                logger.FailedRegisteringPersistedProjectionDefinition(failure, identifier);
            }
        }
    }

    async Task RehydrateOccurrences(EventStoreName eventStore, EventStoreNamespaceName? namespaceName, CancellationToken cancellationToken)
    {
        // Occurrences are independent of event-log data. Discover only unfinished triggers, cap
        // wake-up fan-out, and bound each scope so a slow store cannot prevent the host starting.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            var store = storage.GetEventStore(eventStore);
            var occurrences = namespaceName is null ? store.Occurrences : store.GetNamespace(namespaceName).Occurrences;
            await OccurrenceRecovery.Rehydrate(grainFactory, occurrences, eventStore, namespaceName?.Value, logger, deadline.Token);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Previously recorded work has reminders; a failing namespace must not block others.
            logger.FailedRehydratingOccurrences(exception, eventStore, namespaceName);
        }
    }

    async Task<IReadOnlyList<ObserverKey>> ResolveObserverKeysNeedingAlertBootstrap(EventStoreName eventStore, EventStoreNamespaceName namespaceName)
    {
        var keys = new List<ObserverKey>();
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var definitions = (await eventStoreStorage.Observers.GetAll()).ToDictionary(_ => _.Identifier);
        var states = await eventStoreStorage.GetNamespace(namespaceName).Observers.GetAll();
        var alreadyRehydrated = (await eventStoreStorage.Reducers.GetAll()).Select(_ => (ObserverId)_.Identifier.Value)
            .Concat((await eventStoreStorage.Reactors.GetAll()).Select(_ => (ObserverId)_.Identifier.Value)).ToHashSet();
        foreach (var state in states.Where(_ => IsRequiredObserver(_.Identifier) && !alreadyRehydrated.Contains(_.Identifier)))
        {
            if (!definitions.TryGetValue(state.Identifier, out var definition))
            {
                // Initialized orphans already have key-specific reminders. A legacy record without any definition
                // has no trustworthy event-sequence identity; guessing Log could activate a second writer of it.
                if (state.AlertLifecycleId == Guid.Empty)
                {
                    logger.CannotBootstrapObserverAlerts(state.Identifier, eventStore, namespaceName);
                }

                continue;
            }

            keys.Add(new ObserverKey(state.Identifier, eventStore, namespaceName, definition.EventSequenceId));
        }

        return keys;
    }

    async Task<IReadOnlyList<ObserverKey>> ResolveReducerAndReactorObserverKeys(EventStoreName eventStore, EventStoreNamespaceName namespaceName)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var namespaceStorage = eventStoreStorage.GetNamespace(namespaceName);
        var knownObserverIds = (await namespaceStorage.Observers.GetAll()).Select(_ => _.Identifier).ToHashSet();
        var reducerDefinitions = await eventStoreStorage.Reducers.GetAll();
        var reactorDefinitions = await eventStoreStorage.Reactors.GetAll();

        var reducerObserverKeys = reducerDefinitions
            .Where(_ => knownObserverIds.Contains(_.Identifier))
            .Select(_ => new ObserverKey(_.Identifier, eventStore, namespaceName, _.EventSequenceId));

        var reactorObserverKeys = reactorDefinitions
            .Where(_ => IsRequiredObserver(_.Identifier) && knownObserverIds.Contains(_.Identifier))
            .Select(_ => new ObserverKey(_.Identifier, eventStore, namespaceName, _.EventSequenceId));
        return reducerObserverKeys.Concat(reactorObserverKeys).Distinct().ToArray();
    }
}
