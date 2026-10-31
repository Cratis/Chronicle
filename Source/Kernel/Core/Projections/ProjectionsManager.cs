// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;
using Orleans.Providers;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionsManager"/>.
/// </summary>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="projectionsService"><see cref="IProjectionsServiceClient"/> for managing projections.</param>
/// <param name="projectionDefinitionComparer"><see cref="IProjectionDefinitionComparer"/> for comparing incoming definitions against the registered ones.</param>
/// <param name="languageService"><see cref="Generator"/> for generating projection declaration language strings.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting the local silo details.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> the registration subscribe budget is measured against. Defaults to the system clock.</param>
[ImplicitChannelSubscription]
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ProjectionsManager)]
public partial class ProjectionsManager(
    IProjectionFactory projectionFactory,
    IProjectionsServiceClient projectionsService,
    IProjectionDefinitionComparer projectionDefinitionComparer,
    ILanguageService languageService,
    IStorage storage,
    ILocalSiloDetails localSiloDetails,
    ILogger<ProjectionsManager> logger,
    TimeProvider? timeProvider = null) : Grain<ProjectionsManagerState>, IProjectionsManager, IOnBroadcastChannelSubscribed, IDisposable
{
    /// <summary>
    /// How long a registration waits for its subscribe fan-out before handing what is left to the background repair.
    /// </summary>
    /// <remarks>
    /// This grain is non-reentrant, so a registration holds its turn - and every other registration queues behind it,
    /// including the kernel's own startup registration - for as long as it waits. A client registering a large artifact
    /// set into a store with many namespaces fans out over definitions x namespaces (171 x 37 in production), which
    /// outlasts Orleans' 30 second response timeout. Its caller then times out and retries, and the queue of retries
    /// kept the host from starting. Bounding the wait keeps the turn short: whatever is not subscribed in time is
    /// subscribed by <see cref="ResubscribeMissingProjections"/>, which already repairs any definition x namespace
    /// pair missing from <see cref="_subscribed"/>. Well under the response timeout, to leave room for what a
    /// registration does before and after the fan-out.
    /// </remarks>
    internal static readonly TimeSpan RegistrationSubscribeBudget = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Maximum number of observer subscribe operations this grain lets run concurrently.
    /// </summary>
    /// <remarks>
    /// On boot every ProjectionsManager (one per event store) activates independently and immediately schedules
    /// subscribing every projection to every namespace in full parallel - see
    /// <see cref="SetDefinitionAndSubscribeForProjections"/> fanning out over definitions and
    /// <see cref="SetDefinitionAndSubscribeForProjection"/> fanning out over namespaces. Subscribing an observer also
    /// drives it to recover every failed partition it remembers
    /// (<see cref="IObserver.Subscribe{TObserverSubscriber}"/> and <see cref="IObserver.SubscribeToAllEvents{TObserverSubscriber}"/>
    /// both call <see cref="IObserver.TryRecoverAllFailedPartitions"/>), so with enough projections, namespaces and
    /// accumulated failures across a cluster the unrestrained fan-out from a kernel restart can saturate the silo's
    /// CPU - the recovery storm this bound exists to prevent. The gate sits at the actual subscribe call site
    /// (<see cref="SubscribeIfNotSubscribed"/>) rather than reshaping the fan-out itself, so the bound holds no
    /// matter which caller reaches it: registration, a newly added namespace, or the activation-time resubscribe
    /// timer.
    /// </remarks>
    const int MaxConcurrentSubscriptions = 8;

    readonly SemaphoreSlim _subscriptionThrottle = new(MaxConcurrentSubscriptions, MaxConcurrentSubscriptions);

    readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// The definition x namespace pairs a registration meant to subscribe and, if the observer was retired, revive,
    /// but ran out of budget for. The repair pass revives them instead of skipping the retired observer.
    /// </summary>
    readonly ConcurrentDictionary<(ProjectionId Projection, EventStoreNamespaceName Namespace), byte> _pendingRevival = new();

    /// <summary>
    /// An activation-local hint, never proof that the observer still has its subscription.
    /// </summary>
    readonly ConcurrentDictionary<(ProjectionId Projection, EventStoreNamespaceName Namespace), byte> _subscribed = new();
    readonly Dictionary<EventStoreNamespaceName, Task> _namespaceSubscriptions = new();
    readonly HashSet<EventStoreNamespaceName> _namespaceReruns = [];
    readonly HashSet<Task> _subscriptionPasses = [];
    readonly HashSet<ProjectionId> _removingProjections = [];
    readonly HashSet<EventStoreNamespaceName> _failedNamespaceSetups = new();
    readonly ConcurrentDictionary<ProjectionId, Lazy<Task>> _projectionDefinitions = new();
    readonly Dictionary<ProjectionId, ProjectionDefinition> _registeringDefinitions = new();
    bool _resubscribeRunning;
    DateTimeOffset? _registrationSubscribeDeadline;
    bool _resubscribeRequested;
    HashSet<EventStoreNamespaceName>? _registrationNamespaces;
    bool _skippedWhileRegistering;

    EventStoreName _eventStoreName = EventStoreName.NotSet;
    IGrainTimer? _subscribeTimer;
    IGrainTimer? _resubscribeTimer;

    /// <inheritdoc/>
    public Task Ensure() => Task.CompletedTask;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStoreName = this.GetPrimaryKeyString();
        ScheduleSetDefinitionAndSubscribe();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _subscribeTimer?.Dispose();
        _resubscribeTimer?.Dispose();
        _subscriptionThrottle.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public async Task Register(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner = null, string? registrant = null, string? version = null)
    {
        try
        {
            await RegisterDefinitions(definitions, fullSetOwner, registrant, version);
        }
        finally
        {
            EndAnnouncement();
        }
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ProjectionDefinition>> GetProjectionDefinitions() => Task.FromResult(State.Projections);

    /// <inheritdoc/>
    public async Task<IEnumerable<ProjectionWithDeclaration>> GetProjectionDeclarations()
    {
        var readModelDefinitions = await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions();
        return State.Projections
            .Select(definition =>
            {
                var readModel = readModelDefinitions.SingleOrDefault(rm => rm.Identifier == definition.ReadModel);
                if (readModel is null)
                {
                    logger.MissingReadModelDefinitionForProjection(definition.Identifier, definition.ReadModel);
                    return null;
                }

                return new ProjectionWithDeclaration(
                    definition.Identifier,
                    readModel.ContainerName,
                    languageService.Generate(definition, readModel));
            })
            .Where(_ => _ is not null)
            .Select(_ => _!)
            .ToArray();
    }

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        var eventStore = streamSubscription.ChannelId.GetKeyAsString();
        if (_eventStoreName != eventStore) return Task.CompletedTask;

        streamSubscription.Attach<NamespaceAdded>(OnNamespaceAdded, OnError);
        return Task.CompletedTask;
    }

    List<ProjectionDefinition> FilterUnsupportedKernelDefinitions(List<ProjectionDefinition> definitions)
    {
        var unsupported = definitions.Where(definition => definition.Owner == ProjectionOwner.Kernel && definition.Scope == ProjectionScope.Global).ToArray();
        foreach (var definition in unsupported)
        {
            logger.IgnoringGlobalKernelRegistration(_eventStoreName, definition.Identifier, definition.Scope);
        }

        // Retirement must use the filtered set too: an old silo still includes the global definition in its full set.
        return definitions.Except(unsupported).ToList();
    }

    async Task RegisterDefinitions(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner, string? registrant, string? version)
    {
        var timing = new RegistrationTiming(_clock);
        try
        {
            await RegisterDefinitions(definitions, fullSetOwner, registrant, version, timing);
        }
        finally
        {
            timing.Complete();
            logger.RegistrationTimings(_eventStoreName, registrant ?? string.Empty, timing.Definitions, timing.Changed, timing.Skipped, timing.Total.TotalMilliseconds, timing.Detecting.TotalMilliseconds, timing.Engine.TotalMilliseconds, timing.Subscribing.TotalMilliseconds, timing.Retiring.TotalMilliseconds);
        }
    }

    async Task RegisterDefinitions(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner, string? registrant, string? version, RegistrationTiming timing)
    {
        var namespaces = (await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll()).ToArray();
        var definitionList = FilterUnsupportedKernelDefinitions(StampWithVersion(definitions, version));
        _removingProjections.ExceptWith(definitionList.Select(definition => definition.Identifier));

        Announce(definitionList);

        // A client that reconnects or retries during a rolling deploy can still be the old version. What it registers
        // must neither replace what a newer version registered, nor retire what a newer version added.
        var olderThanStored = definitionList.Where(definition => IsOlderThanStored(definition, version)).ToList();
        if (olderThanStored.Count > 0)
        {
            await IgnoreOlderRegistrations(olderThanStored);
            definitionList = definitionList.Except(olderThanStored).ToList();
            Withdraw(olderThanStored);
        }

        if (fullSetOwner is not null && IsOlderThanAnyRetirableBy(fullSetOwner.Value, registrant, version))
        {
            logger.NotRetiringForOlderRegistration(fullSetOwner.Value, version!);
            fullSetOwner = null;
        }

        var registrantsChanged = RecordRegistrant(definitionList, registrant);

        // Same-version client replicas re-register identical definitions on every startup and reconnect. Handling
        // only what actually changed makes such a re-registration near-free, and because this grain is non-reentrant
        // it also collapses a queue of identical registrations: the first request does the work, every queued
        // duplicate compares equal against the registered state and returns immediately instead of repeating the
        // per-namespace fan-out. Without this, stacked retries kept the request queue from ever draining.
        timing.Definitions = definitionList.Count;
        var changedDefinitions = (await GetChangedDefinitions(definitionList)).ToList();
        var unchanged = definitionList.ExceptBy(changedDefinitions.Select(definition => definition.Identifier), definition => definition.Identifier).ToArray();
        changedDefinitions.AddRange(await GetRetiredDefinitions(unchanged));
        timing.Changed = changedDefinitions.Count;
        timing.Mark(RegistrationPhase.Detecting);

        KeepAnnounced(changedDefinitions);

        var failures = new Dictionary<ProjectionId, Exception>();

        if (changedDefinitions.Count == 0)
        {
            logger.AllDefinitionsUnchanged();
            if (registrantsChanged)
            {
                await WriteStateAsync();
            }
        }
        else
        {
            logger.RegisteringChangedDefinitions(changedDefinitions.Count);
            var acceptedByEngine = await RegisterWithEngine(changedDefinitions, failures);
            timing.Mark(RegistrationPhase.Engine);

            // Subscribe projections immediately so that seeded events appended after registration
            // are not missed due to the asynchronous timer-based subscription scheduling
            _registrationSubscribeDeadline = _clock.GetUtcNow() + RegistrationSubscribeBudget;
            try
            {
                foreach (var (identifier, exception) in await SetDefinitionAndSubscribeForProjections(acceptedByEngine, reactivateRetired: true))
                {
                    failures[identifier] = exception;
                }
            }
            finally
            {
                _registrationSubscribeDeadline = null;
                timing.Skipped = _pendingRevival.Count;
                timing.Mark(RegistrationPhase.Subscribing);
            }

            // Merge fully accepted definitions. Storage upgrades were already retained after SetDefinition:
            // a later namespace failure must not roll them back to legacy defaults. Other failed definitions
            // stay out so the next registration reconciles every layer idempotently.
            MergeIntoState(changedDefinitions.Where(definition => !failures.ContainsKey(definition.Identifier)));
            await WriteStateAsync();
        }

        // Existing namespaces are repaired off the registration turn: a whole-store pass can outlast
        // the caller's timeout. Changed definitions above and newly discovered namespaces below are
        // awaited; unrelated startup repair remains scheduled.
        ScheduleResubscribeProjectionsThatAreNotSubscribed();

        if (fullSetOwner is not null)
        {
            await RetireUnregisteredProjections(definitionList, fullSetOwner.Value, registrant, failures);
        }

        timing.Mark(RegistrationPhase.Retiring);

        if (failures.Count > 0)
        {
            throw new SomeProjectionDefinitionsFailedToRegister(_eventStoreName, failures);
        }

        // Only new namespaces belong to this registration's readiness obligation. Startup repair for
        // existing namespaces remains scheduled, preserving the bounded unchanged-registration path.
        if (_registrationNamespaces is not null)
        {
            await Task.WhenAll(namespaces.Where(namespaceName => !_registrationNamespaces.Contains(namespaceName)).Select(EnsureSubscribedIn));
        }

        _registrationNamespaces = namespaces.ToHashSet();
    }

    /// <summary>
    /// Startup and namespace notifications are documented fresh subscriptions: unlike watchdog repair,
    /// they may end quarantine, but never revive a retired observer.
    /// </summary>
    /// <param name="added">The <see cref="NamespaceAdded"/> notification.</param>
    /// <returns>Awaitable task.</returns>
    Task OnNamespaceAdded(NamespaceAdded added) => SubscribeInNamespace(added.Namespace, reconcile: false);

    async Task<IReadOnlyList<ProjectionDefinition>> GetChangedDefinitions(IEnumerable<ProjectionDefinition> definitions)
    {
        var changed = new List<ProjectionDefinition>();
        foreach (var definition in definitions)
        {
            var existing = State.Projections.FirstOrDefault(p => p.Identifier == definition.Identifier);
            if (existing is null)
            {
                changed.Add(definition);
                continue;
            }

            var compareResult = await projectionDefinitionComparer.Compare(
                new ProjectionKey(definition.Identifier, _eventStoreName),
                existing,
                definition);

            // Metadata and a legacy storage refresh must reach the engine and storage without evolution.
            // A registration without a version never counts as a version change.
            if ((existing.HasLegacyStorageFields && !definition.HasLegacyStorageFields) ||
                compareResult != ProjectionDefinitionCompareResult.Same ||
                !(existing.Tags ?? []).SequenceEqual(definition.Tags ?? []) ||
                (!string.IsNullOrWhiteSpace(definition.Version) && existing.Version != definition.Version))
            {
                changed.Add(definition);
            }
        }

        return changed;
    }

    async Task<IEnumerable<ProjectionDefinition>> GetRetiredDefinitions(ProjectionDefinition[] definitions)
    {
        if (definitions.Length == 0) return [];

        // Only query the submitted unchanged projections, and only return their retired identifiers. Loading
        // every observer's full state makes identical client registrations pay for unrelated observers and failures.
        // The observer's subscription call still makes the authoritative retirement decision inside its own turn.
        var observerIds = definitions.Select(definition => (ObserverId)definition.Identifier).Distinct().ToArray();
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        var retired = new HashSet<ObserverId>();
        foreach (var namespaceName in namespaces)
        {
            retired.UnionWith(await storage.GetEventStore(_eventStoreName).GetNamespace(namespaceName).Observers.GetRetired(observerIds));
        }

        return definitions.Where(definition => retired.Contains(definition.Identifier));
    }

    async Task SetDefinitionAndSubscribeForAllProjections()
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        await Task.WhenAll(namespaces.Select(namespaceName => SubscribeInNamespace(namespaceName, reconcile: false)));
    }

    async Task<IReadOnlyDictionary<ProjectionId, Exception>> SetDefinitionAndSubscribeForProjections(IEnumerable<ProjectionDefinition> definitions, bool reactivateRetired)
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        var readModelDefinitions = (await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions())
            .ToDictionary(readModel => readModel.Identifier);

        // The event type schemas are the same for every definition and namespace in this pass; fetching them once
        // here instead of per subscription keeps a registration from fanning out into definitions × namespaces
        // storage reads.
        var eventTypeSchemas = await storage.GetEventStore(_eventStoreName).EventTypes.GetLatestForAllEventTypes();

        // Failures are isolated per definition: one projection that cannot set its definition or subscribe must not
        // keep the others from landing, and the caller decides whether the collected failures surface as an error
        // (registration) or are only logged (the timer-driven resubscription pass).
        var failures = new ConcurrentDictionary<ProjectionId, Exception>();
        await Task.WhenAll(definitions.Select(async definition =>
        {
            try
            {
                // Timer passes can yield while a registration replaces the captured definitions.
                // Registrations themselves must use their incoming definition, not the old stored one.
                if (!reactivateRetired)
                {
                    var current = State.Projections.FirstOrDefault(projection => projection.Identifier == definition.Identifier);
                    if (current is null || IsBeingReplaced(current)) return;
                    definition = current;
                }

                if (!readModelDefinitions.TryGetValue(definition.ReadModel, out var readModelDefinition))
                {
                    logger.MissingReadModelDefinitionForProjection(definition.Identifier, definition.ReadModel);
                    failures[definition.Identifier] = new ReadModelNotFound(definition.ReadModel);
                    return;
                }

                await SetDefinitionAndSubscribeForProjection(namespaces, definition, readModelDefinition, eventTypeSchemas, reactivateRetired);
            }
            catch (Exception exception)
            {
                failures[definition.Identifier] = exception;
                logger.FailedSettingDefinitionAndSubscribing(exception, definition.Identifier);
            }
        }));

        return failures;
    }

    async Task<IReadOnlyList<ProjectionDefinition>> RegisterWithEngine(IReadOnlyList<ProjectionDefinition> definitions, Dictionary<ProjectionId, Exception> failures)
    {
        var result = await projectionsService.Register(_eventStoreName, definitions);
        if (!result.TryGetError(out var error))
        {
            return definitions;
        }

        foreach (var (identifier, failure) in error.Failures)
        {
            failures[identifier] = failure;
            logger.FailedRegisteringProjectionWithEngine(failure, identifier);
        }

        return definitions.Where(definition => !error.Failures.ContainsKey(definition.Identifier)).ToList();
    }

    void MergeIntoState(IEnumerable<ProjectionDefinition> definitions)
    {
        var projections = State.Projections.ToList();
        foreach (var definition in definitions)
        {
            var existingIndex = projections.FindIndex(p => p.Identifier == definition.Identifier);
            if (existingIndex >= 0)
            {
                projections[existingIndex] = definition;
            }
            else
            {
                projections.Add(definition);
            }
        }

        State.Projections = projections;
    }

    async Task SetDefinitionAndSubscribeForProjection(IEnumerable<EventStoreNamespaceName> namespaces, ProjectionDefinition definition, ReadModelDefinition readModelDefinition, IEnumerable<EventTypeSchema> eventTypeSchemas, bool reactivateRetired)
    {
        ForgetSubscriptionsFor(definition.Identifier);
        logger.SettingDefinition(definition.Identifier);
        await SetProjectionDefinition(definition);

        // SetDefinition has committed the storage upgrade even if a namespace subscription fails next.
        // Keep timer retries from forwarding the old, lossy snapshot back to the authoritative grain.
        if (!definition.HasLegacyStorageFields && State.Projections.Any(existing => existing.Identifier == definition.Identifier && existing.HasLegacyStorageFields))
        {
            MergeIntoState([definition]);
        }

        if (!definition.IsActive)
        {
            return;
        }

        await Task.WhenAll(namespaces.Select(namespaceName => SubscribeIfNotSubscribed(definition, readModelDefinition, namespaceName, eventTypeSchemas, reactivateRetired, boundedByRegistration: reactivateRetired)));
    }

    async Task SubscribeIfNotSubscribed(ProjectionDefinition definition, ReadModelDefinition readModelDefinition, EventStoreNamespaceName namespaceName, IEnumerable<EventTypeSchema> eventTypeSchemas, bool reactivateRetired, bool reconcile = false, bool boundedByRegistration = false)
    {
        if (!definition.IsActive)
        {
            return;
        }

        var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, _eventStoreName, namespaceName, definition.EventSequenceId));
        var pair = (definition.Identifier, namespaceName);

        // Always call Subscribe even when the observer thinks it is already
        // subscribed. For [KeepAlive] grains that survive deactivation
        // collection, the in-memory subscription state can be stale after
        // databases are dropped. Subscribe is idempotent and re-reads
        // persistent state, which detects the reset.
        Task SubscribeAs<TSubscriber>(Engine.IProjection projection)
            where TSubscriber : IObserverSubscriber
        {
            if (reconcile)
            {
                return observer.ReconcileSubscription<TSubscriber>(ObserverType.Projection, projection.EventTypes, localSiloDetails.SiloAddress, definition.SubscribesToAllEvents, definition.GenerationDelivery);
            }

            if (definition.SubscribesToAllEvents)
            {
                return observer.SubscribeToAllEvents<TSubscriber>(ObserverType.Projection, localSiloDetails.SiloAddress, reactivateRetired: reactivateRetired);
            }

            if (definition.GenerationDelivery == EventGenerationDelivery.Pinned)
            {
                return observer.SubscribeWithGenerationDelivery<TSubscriber>(ObserverType.Projection, projection.EventTypes, localSiloDetails.SiloAddress, definition.GenerationDelivery, reactivateRetired: reactivateRetired);
            }

            return observer.Subscribe<TSubscriber>(ObserverType.Projection, projection.EventTypes, localSiloDetails.SiloAddress, reactivateRetired: reactivateRetired);
        }

        // The throttle is taken before the projection is built: building one per definition x namespace pair ahead of
        // the gate ran thousands of them at once on a large registration, CPU the subscribes then had to compete for.
        await _subscriptionThrottle.WaitAsync();
        try
        {
            if (!reactivateRetired && !IsCurrentActiveDefinition(definition)) return;

            // Out of budget: leave the pair for the background repair, which revives it if the observer was retired.
            if (boundedByRegistration && RegistrationSubscribeBudgetSpent())
            {
                _pendingRevival.TryAdd(pair, default);
                return;
            }

            logger.Subscribing(definition.Identifier, namespaceName);
            var projection = await projectionFactory.Create(_eventStoreName, namespaceName, definition, readModelDefinition, eventTypeSchemas);

            logger.SubscribingWithEventTypes(
                definition.Identifier,
                projection.EventTypes.Count(),
                string.Join(", ", projection.EventTypes.Select(et => et.Id)));

            if (reconcile)
            {
                var state = await observer.GetState();
                if (state.AlertDisposition == AlertDisposition.Retired || state.RunningState == ObserverRunningState.Quarantined) return;

                // Namespace reconciliation interleaves with registration and retirement; do not subscribe a stale snapshot.
                if (!IsCurrentActiveDefinition(definition)) return;
            }

            // The subscriber type is how the observer learns whether the projection's partitions may be spread across
            // the silos of a cluster. A projection that can collapse several event sources onto one read model
            // document is serialized by a process-local lock, so all of its partitions must reach one activation.
            await (projection.IsEventSourceKeyed
                ? SubscribeAs<IProjectionObserverSubscriber>(projection)
                : SubscribeAs<ICollapsingProjectionObserverSubscriber>(projection));

            // Only on success - a pair left out here is one the next registration reconciles. Concurrent
            // because the fan-out subscribes definitions and namespaces in parallel.
            if (await observer.IsSubscriptionEstablished())
            {
                _subscribed.TryAdd(pair, default);
                _pendingRevival.TryRemove(pair, out _);
            }
            else
            {
                _subscribed.TryRemove(pair, out _);
            }
        }
        finally
        {
            _subscriptionThrottle.Release();
        }
    }

    bool RegistrationSubscribeBudgetSpent() =>
        _registrationSubscribeDeadline is { } deadline && _clock.GetUtcNow() >= deadline;

    Task OnError(Exception exception) => Task.CompletedTask;

    void ScheduleResubscribeProjectionsThatAreNotSubscribed()
    {
        // Mark pending namespace passes immediately, before a timer can join their stale snapshots.
        // Sets and flags coalesce requests; completion drains at most one pending rerun at a time.
        _namespaceReruns.UnionWith(_namespaceSubscriptions.Keys);
        if (_resubscribeRunning)
        {
            _resubscribeRequested = true;
            return;
        }

        _resubscribeTimer?.Dispose();
        _resubscribeTimer = this.RegisterGrainTimer(
            async _ =>
            {
                _resubscribeTimer?.Dispose();
                _resubscribeTimer = null;

                // Failures are logged by the fan-out itself. Nothing can surface them to the registration that
                // scheduled this - it has already returned - and a dark projection from an earlier activation is
                // not that registration's failure to report.
                await ResubscribeProjectionsThatAreNotSubscribed();
            },

            // Interleaved for the same reason the activation-time pass is: this grain is non-reentrant, so a timer
            // running like a grain call would hold the activation's turn for the whole throttled fan-out and every
            // queued Register would time out behind it. Nothing here needs the turn - the fan-out has its own
            // throttle, the subscribe path is idempotent, and the in-flight guard collapses overlapping passes.
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan, Interleave = true });
    }

    void ScheduleSetDefinitionAndSubscribe()
    {
        _subscribeTimer?.Dispose();
        _subscribeTimer = this.RegisterGrainTimer(
            async _ =>
            {
                _subscribeTimer?.Dispose();
                _subscribeTimer = null;
                await SetDefinitionAndSubscribeForAllProjections();
            },

            // Interleaved on purpose. A grain timer defaults to running like a grain call, which on a
            // non-reentrant grain means holding the activation's turn for the callback's whole duration -
            // and this callback subscribes every projection to every namespace, throttled, with each
            // subscribe also recovering that observer's failed partitions. On a large artifact set that
            // outlasts a caller's response timeout, so every client's Register queued behind it timed out
            // while the work it was waiting for was already being done. Nothing here needs the turn: the
            // fan-out has its own throttle and the subscribe path is idempotent.
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan, Interleave = true });
    }
}
