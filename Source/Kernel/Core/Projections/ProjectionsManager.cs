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
[ImplicitChannelSubscription]
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ProjectionsManager)]
public partial class ProjectionsManager(
    IProjectionFactory projectionFactory,
    IProjectionsServiceClient projectionsService,
    IProjectionDefinitionComparer projectionDefinitionComparer,
    ILanguageService languageService,
    IStorage storage,
    ILocalSiloDetails localSiloDetails,
    ILogger<ProjectionsManager> logger) : Grain<ProjectionsManagerState>, IProjectionsManager, IOnBroadcastChannelSubscribed
{
    EventStoreName _eventStoreName = EventStoreName.NotSet;
    IGrainTimer? _subscribeTimer;

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
    public async Task Register(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner = null)
    {
        var definitionList = definitions.ToList();

        // Same-version client replicas re-register identical definitions on every startup and reconnect. Handling
        // only what actually changed makes such a re-registration near-free, and because this grain is non-reentrant
        // it also collapses a queue of identical registrations: the first request does the work, every queued
        // duplicate compares equal against the registered state and returns immediately instead of repeating the
        // per-namespace fan-out. Without this, stacked retries kept the request queue from ever draining.
        var changedDefinitions = await GetChangedDefinitions(definitionList);
        var failures = new Dictionary<ProjectionId, Exception>();

        if (changedDefinitions.Count == 0)
        {
            logger.AllDefinitionsUnchanged();
        }
        else
        {
            logger.RegisteringChangedDefinitions(changedDefinitions.Count);
            var acceptedByEngine = await RegisterWithEngine(changedDefinitions, failures);

            // Subscribe projections immediately so that seeded events appended after registration
            // are not missed due to the asynchronous timer-based subscription scheduling
            foreach (var (identifier, exception) in await SetDefinitionAndSubscribeForProjections(acceptedByEngine))
            {
                failures[identifier] = exception;
            }

            // Merge into the state only the definitions that the engine and projection grains fully accepted.
            // A downstream subscription failure can happen after the engine accepted a definition. Leaving that
            // definition out makes the next client registration reconcile every layer idempotently without rolling
            // back observers or triggering additional replay side effects during the failed registration.
            MergeIntoState(changedDefinitions.Where(definition => !failures.ContainsKey(definition.Identifier)));
            await WriteStateAsync();
        }

        if (fullSetOwner is not null)
        {
            await RetireUnregisteredProjections(definitionList, fullSetOwner.Value);
        }

        if (failures.Count > 0)
        {
            throw new SomeProjectionDefinitionsFailedToRegister(_eventStoreName, failures);
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

    async Task OnNamespaceAdded(NamespaceAdded added)
    {
        await projectionsService.NamespaceAdded(_eventStoreName, added.Namespace);
        var readModelDefinitions = (await GrainFactory.GetGrain<IReadModelsManager>(_eventStoreName).GetDefinitions())
            .ToDictionary(readModel => readModel.Identifier);
        var eventTypeSchemas = await storage.GetEventStore(_eventStoreName).EventTypes.GetLatestForAllEventTypes();

        await Task.WhenAll(State.Projections.Select(async projectionDefinition =>
        {
            try
            {
                var key = new ProjectionKey(projectionDefinition.Identifier, _eventStoreName);
                var projection = GrainFactory.GetGrain<IProjection>(key);
                await projection.SetDefinition(projectionDefinition);
                if (!readModelDefinitions.TryGetValue(projectionDefinition.ReadModel, out var readModelDefinition))
                {
                    logger.MissingReadModelDefinitionForProjection(projectionDefinition.Identifier, projectionDefinition.ReadModel);
                    return;
                }

                await SubscribeIfNotSubscribed(projectionDefinition, readModelDefinition, added.Namespace, eventTypeSchemas);
            }
            catch (Exception exception)
            {
                // One projection that cannot subscribe in the new namespace must not keep the others from doing so.
                logger.FailedSettingDefinitionAndSubscribing(exception, projectionDefinition.Identifier);
            }
        }));
    }

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
            if (compareResult != ProjectionDefinitionCompareResult.Same)
            {
                changed.Add(definition);
            }
        }

        return changed;
    }

    async Task SetDefinitionAndSubscribeForAllProjections()
    {
        await SetDefinitionAndSubscribeForProjections(State.Projections);
    }

    async Task<IReadOnlyDictionary<ProjectionId, Exception>> SetDefinitionAndSubscribeForProjections(IEnumerable<ProjectionDefinition> definitions)
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
                if (!readModelDefinitions.TryGetValue(definition.ReadModel, out var readModelDefinition))
                {
                    logger.MissingReadModelDefinitionForProjection(definition.Identifier, definition.ReadModel);
                    failures[definition.Identifier] = new ReadModelNotFound(definition.ReadModel);
                    return;
                }

                await SetDefinitionAndSubscribeForProjection(namespaces, definition, readModelDefinition, eventTypeSchemas);
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
        try
        {
            await projectionsService.Register(_eventStoreName, definitions);
            return definitions;
        }
        catch (Exception exception) when (ProjectionDefinitionsRegistrationFailed.TryFindFailures(exception, out var registrationFailures))
        {
            foreach (var (identifier, failure) in registrationFailures)
            {
                failures[identifier] = failure;
                logger.FailedRegisteringProjectionWithEngine(failure, identifier);
            }

            return definitions.Where(definition => !registrationFailures.ContainsKey(definition.Identifier)).ToList();
        }
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

    async Task SetDefinitionAndSubscribeForProjection(IEnumerable<EventStoreNamespaceName> namespaces, ProjectionDefinition definition, ReadModelDefinition readModelDefinition, IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        logger.SettingDefinition(definition.Identifier);
        var key = new ProjectionKey(definition.Identifier, _eventStoreName);
        var projection = GrainFactory.GetGrain<IProjection>(key);
        await projection.SetDefinition(definition);

        if (!definition.IsActive)
        {
            return;
        }

        await Task.WhenAll(namespaces.Select(namespaceName => SubscribeIfNotSubscribed(definition, readModelDefinition, namespaceName, eventTypeSchemas)));
    }

    async Task SubscribeIfNotSubscribed(ProjectionDefinition definition, ReadModelDefinition readModelDefinition, EventStoreNamespaceName namespaceName, IEnumerable<EventTypeSchema> eventTypeSchemas)
    {
        if (!definition.IsActive)
        {
            return;
        }

        var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(definition.Identifier, _eventStoreName, namespaceName, definition.EventSequenceId));

        logger.Subscribing(definition.Identifier, namespaceName);
        var projection = await projectionFactory.Create(_eventStoreName, namespaceName, definition, readModelDefinition, eventTypeSchemas);

        logger.SubscribingWithEventTypes(
            definition.Identifier,
            projection.EventTypes.Count(),
            string.Join(", ", projection.EventTypes.Select(et => et.Id)));

        // Always call Subscribe even when the observer thinks it is already
        // subscribed. For [KeepAlive] grains that survive deactivation
        // collection, the in-memory subscription state can be stale after
        // databases are dropped. Subscribe is idempotent and re-reads
        // persistent state, which detects the reset.
        Task SubscribeAs<TSubscriber>()
            where TSubscriber : IObserverSubscriber =>
            definition.SubscribesToAllEvents
                ? observer.SubscribeToAllEvents<TSubscriber>(
                    ObserverType.Projection,
                    localSiloDetails.SiloAddress)
                : observer.Subscribe<TSubscriber>(
                    ObserverType.Projection,
                    projection.EventTypes,
                    localSiloDetails.SiloAddress);

        // The subscriber type is how the observer learns whether the projection's partitions may be spread across
        // the silos of a cluster. A projection that can collapse several event sources onto one read model
        // document is serialized by a process-local lock, so all of its partitions must reach one activation.
        await (projection.IsEventSourceKeyed
            ? SubscribeAs<IProjectionObserverSubscriber>()
            : SubscribeAs<ICollapsingProjectionObserverSubscriber>());
    }

    Task OnError(Exception exception) => Task.CompletedTask;

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
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan });
    }
}
