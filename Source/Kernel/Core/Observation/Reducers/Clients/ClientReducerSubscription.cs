// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientReducerSubscription"/>.
/// </summary>
/// <remarks>
/// Operations run one at a time, in the order they are asked for, which is what the grain it replaces guaranteed with its
/// turns: a registration the client retries while the first attempt is still running waits for it instead of racing it.
/// </remarks>
/// <param name="observerKey">The <see cref="ConnectedObserverKey"/> identifying the reducer on the connection.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for reaching the observer and the connected clients.</param>
/// <param name="reducerDefinitionComparer"><see cref="IReducerDefinitionComparer"/> for comparing reducer definitions.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo the stream is on.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for accessing Chronicle configuration.</param>
/// <param name="storage">The <see cref="IStorage"/> holding the shared reducer definitions.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public sealed class ClientReducerSubscription(
    ConnectedObserverKey observerKey,
    IGrainFactory grainFactory,
    IReducerDefinitionComparer reducerDefinitionComparer,
    ILocalSiloDetails localSiloDetails,
    IOptions<ChronicleOptions> options,
    IStorage storage,
    ILogger<ClientReducerSubscription> logger) : IClientReducerSubscription
{
    readonly AsyncGate _turn = new();
    IObserver? _observer;
    ReducerDefinition? _deferredDefinition;
    bool _subscribed;

    /// <summary>
    /// Gets the replay the last definition change scheduled, for callers that need to wait for it.
    /// </summary>
    internal Task ScheduledReplay { get; private set; } = Task.CompletedTask;

    /// <inheritdoc/>
    public async Task SetDefinitionAndSubscribe(ReducerDefinition definition)
    {
        await _turn.WaitAsync();
        try
        {
            await SetDefinitionAndSubscribeInTurn(definition);
        }
        finally
        {
            _turn.Release();
        }
    }

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        await _turn.WaitAsync();
        try
        {
            if (!_subscribed)
            {
                return;
            }

            logger.ClientDisconnected(
                observerKey.ConnectionId,
                observerKey.EventStore,
                observerKey.ObserverId,
                observerKey.EventSequenceId,
                observerKey.Namespace);

            var key = new ObserverKey(observerKey.ObserverId, observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId);
            var observer = grainFactory.GetGrain<IObserver>(key);

            // Atomic check-and-unsubscribe inside the Observer grain. Doing the check (GetSubscription)
            // and the action (Unsubscribe) as two separate grain calls would race against a new client's
            // Subscribe that lands between them — the old client's Unsubscribe would then destroy the
            // new client's subscription.
            await observer.UnsubscribeIfMatchesClient(observerKey.ConnectionId);
            _subscribed = false;
        }
        finally
        {
            _turn.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsSubscriptionIntact()
    {
        await _turn.WaitAsync();
        try
        {
            // Anything that needs the definition - a registration waiting for a webhook to leave, an observer served by a
            // webhook, a client the observer has lost, or failed setup - answers false, and the full check handles it. So does a client the
            // observer only knows as a superseded registration: it is not receiving anything, and a promotion that fails
            // when the client that replaced it leaves would otherwise go unnoticed until the safety net.
            if (_deferredDefinition is not null)
            {
                return false;
            }

            _observer ??= grainFactory.GetGrain<IObserver>(new ObserverKey(observerKey.ObserverId, observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId));
            if (!await _observer.IsClientTarget(observerKey.ConnectionId) || !await _observer.IsSubscriptionEstablished())
            {
                return false;
            }

            _subscribed = true;
            return true;
        }
        finally
        {
            _turn.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsRegistrationDeferred()
    {
        await _turn.WaitAsync();
        try
        {
            return _deferredDefinition is not null;
        }
        finally
        {
            _turn.Release();
        }
    }

    /// <inheritdoc/>
    public async Task KeepSubscription(ReducerDefinition definition)
    {
        await _turn.WaitAsync();
        try
        {
            await KeepSubscriptionInTurn(definition);
        }
        finally
        {
            _turn.Release();
        }
    }

    async Task SetDefinitionAndSubscribeInTurn(ReducerDefinition definition)
    {
        var key = new ReducerKey(observerKey.ObserverId, observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId);
        _observer = grainFactory.GetGrain<IObserver>(new ObserverKey(key.ReducerId, key.EventStore, key.Namespace, key.EventSequenceId));
        if (await DeferRegistrationIfServedByWebhook(definition))
        {
            return;
        }

        // The stored definition is shared by every connection registering the reducer, so it is read for each
        // registration rather than remembered: another connection may have stored a different one since.
        var reducers = storage.GetEventStore(key.EventStore).Reducers;
        var stored = await reducers.Has(key.ReducerId) ? await reducers.Get(key.ReducerId) : null;
        var previousDefinition = stored ?? definition with { EventTypes = [] };
        var compareResult = await reducerDefinitionComparer.Compare(key, previousDefinition, definition);

        var isUnchanged = stored is not null && ReducerDefinitionIdentity.IsIdentical(stored, definition);
        if (!isUnchanged)
        {
            await reducers.Save(definition);
        }

        if (compareResult == ReducerDefinitionCompareResult.Different)
        {
            if (_subscribed)
            {
                await _observer.Unsubscribe();
                _subscribed = false;
            }
            var namespaceNames = (await grainFactory.GetGrain<INamespaces>(key.EventStore).GetAll()).ToList();

            await EvolveForAllNamespaces(key, namespaceNames, previousDefinition, definition);
        }

        if (!_subscribed && definition.IsActive)
        {
            await SubscribeToObserver(key, definition);
        }
        else if (_subscribed && !definition.IsActive)
        {
            await _observer.Unsubscribe();
            _subscribed = false;
        }
    }

    async Task KeepSubscriptionInTurn(ReducerDefinition definition)
    {
        if (await CompleteDeferredRegistration())
        {
            return;
        }

        if (!definition.IsActive)
        {
            return;
        }

        var key = new ObserverKey(observerKey.ObserverId, observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId);
        _observer ??= grainFactory.GetGrain<IObserver>(key);
        if (await WaitIfServedByWebhook())
        {
            return;
        }

        if (await NeedsRestoration())
        {
            var owner = await FindConnectionOwner();
            if (owner is null)
            {
                return;
            }

            var connectedClient = await owner.GetConnectedClient(observerKey.ConnectionId);
            logger.RestoringLostClientSubscription(observerKey.ObserverId, observerKey.ConnectionId);
            try
            {
                if (definition.PageDelivery is not null)
                {
                    await _observer.RestoreClientSubscription<IReducerPageObserverSubscriber>(
                        ObserverType.Reducer,
                        definition.EventTypes.Select(_ => _.EventType).ToArray(),
                        localSiloDetails.SiloAddress,
                        connectedClient,
                        filters: definition.Filters,
                        generationDelivery: definition.GenerationDelivery);
                }
                else
                {
                    await _observer.RestoreClientSubscription<IReducerObserverSubscriber>(
                        ObserverType.Reducer,
                        definition.EventTypes.Select(_ => _.EventType).ToArray(),
                        localSiloDetails.SiloAddress,
                        connectedClient,
                        filters: definition.Filters,
                        generationDelivery: definition.GenerationDelivery);
                }
                await RecordObserver();
            }
            catch (ObserverServedByAnotherTransport)
            {
                WaitForWebhook();
                return;
            }
        }

        _subscribed = true;
    }

    async Task<bool> NeedsRestoration() =>
        !await _observer!.KnowsClient(observerKey.ConnectionId) || !await _observer.IsSubscriptionEstablished();

    async Task<bool> DeferRegistrationIfServedByWebhook(ReducerDefinition definition)
    {
        var waiting = await WaitIfServedByWebhook();
        _deferredDefinition = waiting ? definition : null;
        return waiting;
    }

    async Task<bool> CompleteDeferredRegistration()
    {
        if (_deferredDefinition is not { } definition)
        {
            return false;
        }

        await SetDefinitionAndSubscribeInTurn(definition);
        return true;
    }

    async Task<bool> WaitIfServedByWebhook()
    {
        if (!(await _observer!.GetSubscription()).IsServedByWebhook)
        {
            return false;
        }

        WaitForWebhook();
        return true;
    }

    void WaitForWebhook()
    {
        _subscribed = false;
        logger.WaitingForWebhook(observerKey.ObserverId);
    }

    /// <summary>The connection may be registered on another silo than the one its streams reach, so every silo is considered.</summary>
    Task<IConnectedClients?> FindConnectionOwner() =>
        ConnectedClientsLocator.Find(grainFactory, localSiloDetails.SiloAddress, observerKey.ConnectionId);

    /// <summary>Lets the connection registry tell this observer when the connection is dropped, instead of leaving it to the watchdog.</summary>
    /// <remarks>Best effort: a failure (for example during a rolling upgrade) leaves takeover to the watchdog and never fails the subscription.</remarks>
    async Task RecordObserver()
    {
        try
        {
            var owner = await FindConnectionOwner();
            if (owner is null)
            {
                return;
            }

            await owner.RecordObserver(
                observerKey.ConnectionId,
                new ObserverKey(observerKey.ObserverId, observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.FailedToRecordObserver(observerKey.ObserverId, observerKey.ConnectionId, ex);
        }
    }

    async Task SubscribeToObserver(ReducerKey key, ReducerDefinition definition)
    {
        _observer = grainFactory.GetGrain<IObserver>(new ObserverKey(key.ReducerId, key.EventStore, key.Namespace, key.EventSequenceId));
        var owner = await FindConnectionOwner() ?? throw new ClientIsNotConnected(observerKey.ConnectionId);
        var connectedClient = await owner.GetConnectedClient(observerKey.ConnectionId);

        try
        {
            var deliversPages = definition.PageDelivery is not null;
            if (definition.GenerationDelivery == Concepts.Observation.EventGenerationDelivery.Pinned && deliversPages)
            {
                await _observer.SubscribeWithGenerationDelivery<IReducerPageObserverSubscriber>(
                    ObserverType.Reducer,
                    definition.EventTypes.Select(_ => _.EventType).ToArray(),
                    localSiloDetails.SiloAddress,
                    definition.GenerationDelivery,
                    connectedClient,
                    filters: definition.Filters);
            }
            else if (definition.GenerationDelivery == Concepts.Observation.EventGenerationDelivery.Pinned)
            {
                await _observer.SubscribeWithGenerationDelivery<IReducerObserverSubscriber>(
                    ObserverType.Reducer,
                    definition.EventTypes.Select(_ => _.EventType).ToArray(),
                    localSiloDetails.SiloAddress,
                    definition.GenerationDelivery,
                    connectedClient,
                    filters: definition.Filters);
            }
            else if (deliversPages)
            {
                await _observer.Subscribe<IReducerPageObserverSubscriber>(
                    ObserverType.Reducer,
                    definition.EventTypes.Select(_ => _.EventType).ToArray(),
                    localSiloDetails.SiloAddress,
                    connectedClient,
                    filters: definition.Filters);
            }
            else
            {
                await _observer.Subscribe<IReducerObserverSubscriber>(
                    ObserverType.Reducer,
                    definition.EventTypes.Select(_ => _.EventType).ToArray(),
                    localSiloDetails.SiloAddress,
                    connectedClient,
                    filters: definition.Filters);
            }

            await RecordObserver();
            _subscribed = true;
        }
        catch (ObserverServedByAnotherTransport)
        {
            WaitForWebhook();
        }
    }

    async Task EvolveForAllNamespaces(
        ReducerKey key,
        IEnumerable<EventStoreNamespaceName> namespaces,
        ReducerDefinition previousDefinition,
        ReducerDefinition currentDefinition)
    {
        var addedEventTypes = ReducerDefinitionEvolution.GetAddedEventTypesIfOnlyEventTypesChanged(previousDefinition, currentDefinition);
        var policy = options.Value.Observers.DefinitionEvolution;

        foreach (var @namespace in namespaces)
        {
            var plan = DefinitionEvolutionPlan.FullReplay;
            if (addedEventTypes.Length > 0)
            {
                var eventSequence = storage.GetEventStore(key.EventStore).GetNamespace(@namespace).GetEventSequence(currentDefinition.EventSequenceId);

                var existingEventTypes = previousDefinition.EventTypes.Select(_ => _.EventType);
                var usesEventSourcePartitions = currentDefinition.EventTypes.All(_ => _.Key.Value == WellKnownExpressions.EventSourceId);
                plan = await DefinitionEvolutionPlan.ForAddedReducerEventTypes(
                    eventSequence,
                    addedEventTypes,
                    existingEventTypes,
                    usesEventSourcePartitions);
            }

            switch (plan.Operation)
            {
                case DefinitionEvolutionOperation.NoAction:
                    logger.ReducerEvolutionNeedsNoAction(key.EventStore, key.ReducerId, key.EventSequenceId, @namespace);
                    break;
                case DefinitionEvolutionOperation.PartialReplay when policy.Allows(plan.Operation):
                    ScheduleReplay(() => ReplayReducerPartitions(key, @namespace, plan.AffectedEventSources, plan.AffectedEventTypes));
                    break;
                case DefinitionEvolutionOperation.FullReplay when policy.Allows(plan.Operation):
                    ScheduleReplay(() => ReplayReducer(key, @namespace));
                    break;
                default:
                    await AddReplayRecommendation(key, @namespace, currentDefinition);
                    break;
            }
        }
    }

    async Task ReplayReducer(ReducerKey key, EventStoreNamespaceName @namespace)
    {
        logger.AutoReplayingReducer(key.EventStore, key.ReducerId, key.EventSequenceId, @namespace);
        var observer = grainFactory.GetGrain<IObserver>(new ObserverKey(key.ReducerId, key.EventStore, @namespace, key.EventSequenceId));
        await observer.Replay();
    }

    async Task ReplayReducerPartitions(
        ReducerKey key,
        EventStoreNamespaceName @namespace,
        IEnumerable<EventSourceId> eventSourceIds,
        IEnumerable<EventType> eventTypes)
    {
        var affectedEventSources = eventSourceIds.ToArray();
        logger.PartiallyReplayingReducer(key.EventStore, key.ReducerId, key.EventSequenceId, @namespace, affectedEventSources.Length);
        var observer = grainFactory.GetGrain<IObserver>(new ObserverKey(key.ReducerId, key.EventStore, @namespace, key.EventSequenceId));
        foreach (var eventSourceId in affectedEventSources)
        {
            await observer.ReplayPartition(eventSourceId, EventTypeSet.From(eventTypes));
        }
    }

    async Task AddReplayRecommendation(ReducerKey key, EventStoreNamespaceName @namespace, ReducerDefinition currentDefinition)
    {
        var recommendationsManager = grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(key.EventStore, @namespace));
        await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
            "Reducer definition has changed.",
            new()
            {
                ObserverId = key.ReducerId,
                ObserverKey = new(key.ReducerId, key.EventStore, @namespace, key.EventSequenceId),
                ObserverType = ObserverType.Reducer,
                ChangeId = DefinitionChangeIdentity.For(currentDefinition),
                Reasons = [new ReducerDefinitionChangedReplayCandidateReason()]
            });
    }

    void ScheduleReplay(Func<Task> work) =>
        ScheduledReplay = Task.Run(async () =>
        {
            try
            {
                await work();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.FailedToReplayAfterDefinitionChange(observerKey.ObserverId, ex);
            }
        });
}
