// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientReactorSubscription"/>.
/// </summary>
/// <remarks>
/// Operations run one at a time, in the order they are asked for, which is what the grain it replaces guaranteed with its
/// turns: a registration the client retries while the first attempt is still running waits for it instead of racing it.
/// </remarks>
/// <param name="observerKey">The <see cref="ConnectedObserverKey"/> identifying the reactor on the connection.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for reaching the observer and the connected clients.</param>
/// <param name="reactorDefinitionComparer"><see cref="IReactorDefinitionComparer"/> for comparing reactor definitions.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo the stream is on.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for accessing Chronicle configuration.</param>
/// <param name="storage">The <see cref="IStorage"/> holding the shared reactor definitions.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public sealed class ClientReactorSubscription(
    ConnectedObserverKey observerKey,
    IGrainFactory grainFactory,
    IReactorDefinitionComparer reactorDefinitionComparer,
    ILocalSiloDetails localSiloDetails,
    IOptions<ChronicleOptions> options,
    IStorage storage,
    ILogger<ClientReactorSubscription> logger) : IClientReactorSubscription
{
    readonly AsyncGate _turn = new();
    IObserver? _observer;
    ReactorDefinition? _deferredDefinition;
    bool _subscribed;

    /// <summary>
    /// Gets the replay the last definition change scheduled, for callers that need to wait for it.
    /// </summary>
    internal Task ScheduledReplay { get; private set; } = Task.CompletedTask;

    /// <inheritdoc/>
    public async Task SetDefinitionAndSubscribe(ReactorDefinition definition)
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
            // webhook, a client the observer has lost - answers false, and the full check handles it. So does a client the
            // observer only knows as a superseded registration: it is not receiving anything, and a promotion that fails
            // when the client that replaced it leaves would otherwise go unnoticed until the safety net.
            if (_deferredDefinition is not null)
            {
                return false;
            }

            _observer ??= grainFactory.GetGrain<IObserver>(new ReactorKey(observerKey.ObserverId, observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId));
            if (!await _observer.IsClientTarget(observerKey.ConnectionId))
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
    public async Task KeepSubscription(ReactorDefinition definition)
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

    async Task SetDefinitionAndSubscribeInTurn(ReactorDefinition definition)
    {
        logger.Starting(observerKey.EventStore, observerKey.ObserverId, observerKey.EventSequenceId, observerKey.Namespace);
        var key = new ReactorKey(observerKey.ObserverId, observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId);
        _observer = grainFactory.GetGrain<IObserver>(key);
        if (await DeferRegistrationIfServedByWebhook(definition))
        {
            return;
        }

        // The stored definition is shared by every connection registering the reactor, so it is read for each
        // registration rather than remembered: another connection may have stored a different one since.
        var reactors = storage.GetEventStore(key.EventStore).Reactors;
        var stored = await reactors.Has(key.ReactorId) ? await reactors.Get(key.ReactorId) : null;
        var previousDefinition = stored ?? definition with { EventTypes = [] };
        var compareResult = await reactorDefinitionComparer.Compare(key, previousDefinition, definition);

        var isUnchanged = stored is not null && ReactorDefinitionIdentity.IsIdentical(stored, definition);
        if (!isUnchanged)
        {
            await reactors.Save(definition);
        }

        if (compareResult == ReactorDefinitionCompareResult.Different)
        {
            if (_subscribed)
            {
                await _observer.Unsubscribe();
                _subscribed = false;
            }
            var namespaceNames = (await grainFactory.GetGrain<INamespaces>(key.EventStore).GetAll()).ToList();

            // Schedule replay separately so that the registration returns immediately,
            // keeping registration fast and replay as a separate concern.
            if (options.Value.Observers.ReplayOnDefinitionChange)
            {
                ScheduleReplay(() => ReplayForAllNamespaces(key, namespaceNames));
            }
            else
            {
                await AddReplayRecommendationForAllNamespaces(key, namespaceNames, definition);
            }
        }

        if (!_subscribed)
        {
            await SubscribeToObserver(key, definition);
        }
    }

    async Task KeepSubscriptionInTurn(ReactorDefinition definition)
    {
        if (await CompleteDeferredRegistration())
        {
            return;
        }

        var key = new ReactorKey(observerKey.ObserverId, observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId);
        _observer ??= grainFactory.GetGrain<IObserver>(key);
        if (await WaitIfServedByWebhook())
        {
            return;
        }

        if (!await _observer.KnowsClient(observerKey.ConnectionId))
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
                    await _observer.RestoreClientSubscription<IReactorPageObserverSubscriber>(
                    ObserverType.Reactor,
                    definition.EventTypes.Select(e => e.EventType).ToArray(),
                    localSiloDetails.SiloAddress,
                    connectedClient,
                    definition.IsReplayable,
                    definition.Filters,
                    definition.GenerationDelivery);
                }
                else
                {
                    await _observer.RestoreClientSubscription<IReactorObserverSubscriber>(
                    ObserverType.Reactor,
                    definition.EventTypes.Select(e => e.EventType).ToArray(),
                    localSiloDetails.SiloAddress,
                    connectedClient,
                    definition.IsReplayable,
                    definition.Filters,
                    definition.GenerationDelivery);
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

    async Task<bool> DeferRegistrationIfServedByWebhook(ReactorDefinition definition)
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

    Task<IConnectedClients?> FindConnectionOwner() =>
        ConnectedClientsLocator.Find(grainFactory, localSiloDetails.SiloAddress, observerKey.ConnectionId);

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

    async Task SubscribeToObserver(ReactorKey key, ReactorDefinition definition)
    {
        _observer = grainFactory.GetGrain<IObserver>(key);
        var owner = await FindConnectionOwner() ?? throw new ClientIsNotConnected(observerKey.ConnectionId);
        var connectedClient = await owner.GetConnectedClient(observerKey.ConnectionId);
        var eventTypes = definition.EventTypes.Select(e => e.EventType).ToArray();
        if (definition.StartAtTail)
        {
            await _observer.StartAtTailWhenNew();
        }

        try
        {
            var deliversPages = definition.PageDelivery is not null;
            if (definition.GenerationDelivery == Concepts.Observation.EventGenerationDelivery.Pinned && deliversPages)
            {
                await _observer.SubscribeWithGenerationDelivery<IReactorPageObserverSubscriber>(
                    ObserverType.Reactor,
                    eventTypes,
                    localSiloDetails.SiloAddress,
                    definition.GenerationDelivery,
                    connectedClient,
                    definition.IsReplayable,
                    definition.Filters);
            }
            else if (definition.GenerationDelivery == Concepts.Observation.EventGenerationDelivery.Pinned)
            {
                await _observer.SubscribeWithGenerationDelivery<IReactorObserverSubscriber>(
                    ObserverType.Reactor,
                    eventTypes,
                    localSiloDetails.SiloAddress,
                    definition.GenerationDelivery,
                    connectedClient,
                    definition.IsReplayable,
                    definition.Filters);
            }
            else if (deliversPages)
            {
                await _observer.Subscribe<IReactorPageObserverSubscriber>(
                    ObserverType.Reactor,
                    eventTypes,
                    localSiloDetails.SiloAddress,
                    connectedClient,
                    definition.IsReplayable,
                    definition.Filters);
            }
            else
            {
                await _observer.Subscribe<IReactorObserverSubscriber>(
                    ObserverType.Reactor,
                    eventTypes,
                    localSiloDetails.SiloAddress,
                    connectedClient,
                    definition.IsReplayable,
                    definition.Filters);
            }

            await RecordObserver();
            _subscribed = true;
        }
        catch (ObserverServedByAnotherTransport)
        {
            WaitForWebhook();
        }
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

    async Task ReplayForAllNamespaces(ReactorKey key, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            logger.AutoReplayingReactor(key.EventStore, key.ReactorId, key.EventSequenceId, @namespace);
            var observer = grainFactory.GetGrain<IObserver>(new ObserverKey(key.ReactorId, key.EventStore, @namespace, key.EventSequenceId));
            await observer.Replay();
        }
    }

    async Task AddReplayRecommendationForAllNamespaces(ReactorKey key, IEnumerable<EventStoreNamespaceName> namespaces, ReactorDefinition definition)
    {
        foreach (var @namespace in namespaces)
        {
            var recommendationsManager = grainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(key.EventStore, @namespace));
            await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                "Reducer definition has changed.",
                new()
                {
                    ObserverId = key.ReactorId,
                    ObserverKey = new(key.ReactorId, key.EventStore, @namespace, key.EventSequenceId),
                    ObserverType = ObserverType.Reactor,
                    ChangeId = DefinitionChangeIdentity.For(definition),
                    Reasons = [new ReactorDefinitionChangedReplayCandidateReason()]
                });
        }
    }
}
