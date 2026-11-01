// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
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
using Orleans.Placement;
using Orleans.Providers;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReactor"/>.
/// </summary>
/// <param name="reactorDefinitionComparer"><see cref="IReactorDefinitionComparer"/> for comparing reactor definitions.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for accessing Chronicle configuration.</param>
/// <param name="storage"><see cref="IStorage"/> containing observer definitions.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reactors)]
[PreferLocalPlacement]
public class Reactor(
    IReactorDefinitionComparer reactorDefinitionComparer,
    ILocalSiloDetails localSiloDetails,
    IOptions<ChronicleOptions> options,
    IStorage storage,
    ILogger<Reactor> logger) : Grain<ReactorDefinition>, IReactor
{
    IConnectedClients? _connectedClients;
    ConnectedObserverKey? _observerKey;
    IObserver? _observer;
    ReactorDefinition? _deferredDefinition;
    ObserverRegistrationConflictRequest? _standbyFor;
    bool _subscribed;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _connectedClients = GrainFactory.GetConnectedClients(localSiloDetails.SiloAddress);

        _observerKey = ConnectedObserverKey.Parse(this.GetPrimaryKeyString());

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SetDefinitionAndSubscribe(ReactorDefinition definition)
    {
        logger.Starting(_observerKey!.EventStore, _observerKey.ObserverId!, _observerKey!.EventSequenceId, _observerKey!.Namespace);
        var key = new ReactorKey(_observerKey.ObserverId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
        _observer = GrainFactory.GetGrain<IObserver>(key);
        if (await DeferRegistrationIfServedByWebhook(definition))
        {
            return;
        }

        var connectedClient = await _connectedClients!.GetConnectedClient(_observerKey.ConnectionId);
        var owning = await ObserverTransportSubscriptions.OnOtherSequence(storage, GrainFactory, new ObserverKey(key.ReactorId, key.EventStore, key.Namespace, key.EventSequenceId));
        var precedence = ObserverRegistrationPrecedence.Evaluate(connectedClient, owning);
        if (precedence.Outcome == RegistrationPrecedenceOutcome.OwnerIsNewer)
        {
            await EnterStandby(definition, precedence, connectedClient.Version);
            return;
        }

        var compareResult = await reactorDefinitionComparer.Compare(key, State, definition);

        State = definition;
        await WriteStateAsync();

        if (compareResult == ReactorDefinitionCompareResult.Different)
        {
            if (_subscribed)
            {
                await _observer!.Unsubscribe();
                _subscribed = false;
            }
            var namespaceNames = (await GrainFactory.GetGrain<INamespaces>(key.EventStore).GetAll()).ToList();

            // Schedule replay as a separate grain turn so that SetDefinition() returns immediately,
            // keeping registration fast and replay as a separate concern.
            if (options.Value.Observers.ReplayOnDefinitionChange)
            {
                this.ScheduleInSeparateTurn(() => ReplayForAllNamespaces(key, namespaceNames));
            }
            else
            {
                await AddReplayRecommendationForAllNamespaces(key, namespaceNames);
            }
        }

        if (!_subscribed)
        {
            await SubscribeToObserver(key, definition, connectedClient);
        }
    }

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        logger.ClientDisconnected(
            _observerKey!.ConnectionId,
            _observerKey!.EventStore,
            _observerKey.ObserverId!,
            _observerKey!.EventSequenceId,
            _observerKey!.Namespace);

        var key = new ObserverKey(_observerKey.ObserverId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
        var observer = GrainFactory.GetGrain<IObserver>(key);

        // Atomic check-and-unsubscribe inside the Observer grain. Doing the check (GetSubscription)
        // and the action (Unsubscribe) as two separate grain calls would race against a new client's
        // Subscribe that lands between them — the old client's Unsubscribe would then destroy the
        // new client's subscription.
        await observer.UnsubscribeIfMatchesClient(_observerKey!.ConnectionId);

        _subscribed = false;
    }

    /// <inheritdoc/>
    public async Task KeepSubscription(ReactorDefinition definition)
    {
        if (await CompleteDeferredRegistration())
        {
            return;
        }

        var key = new ReactorKey(_observerKey!.ObserverId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
        _observer ??= GrainFactory.GetGrain<IObserver>(key);
        if (await WaitIfServedByWebhook() || await FollowDefinitionOnAnotherSequence(definition))
        {
            return;
        }

        if (!await _observer.KnowsClient(_observerKey.ConnectionId))
        {
            if (!await _connectedClients!.IsConnected(_observerKey.ConnectionId))
            {
                return;
            }

            var connectedClient = await _connectedClients!.GetConnectedClient(_observerKey.ConnectionId);
            logger.RestoringLostClientSubscription(_observerKey.ObserverId, _observerKey.ConnectionId);
            try
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
            catch (ObserverOwnedByNewerClientVersion exception)
            {
                await EnterStandby(definition, new(RegistrationPrecedenceOutcome.OwnerIsNewer, exception.OwnerSequence, exception.OwnerVersion), exception.IncomingVersion);
                return;
            }
            catch (ObserverServedByAnotherTransport)
            {
                WaitForWebhook();
                return;
            }
        }

        _subscribed = true;
        _standbyFor = null;
    }

    async Task EnterStandby(ReactorDefinition definition, RegistrationPrecedence precedence, string incomingVersion)
    {
        _deferredDefinition = definition;
        _subscribed = false;
        if (_standbyFor is { } previous && previous.OwnerEventSequenceId == precedence.OwnerSequence &&
            previous.OwnerVersion == precedence.OwnerVersion && previous.IncomingVersion == incomingVersion)
        {
            return;
        }

        _standbyFor = new()
        {
            ObserverId = _observerKey!.ObserverId,
            IncomingEventSequenceId = _observerKey.EventSequenceId,
            OwnerEventSequenceId = precedence.OwnerSequence!,
            IncomingVersion = incomingVersion,
            OwnerVersion = precedence.OwnerVersion!,
            Outcome = "standby"
        };
        logger.RegistrationIgnored(_observerKey.ObserverId, _observerKey.EventSequenceId, incomingVersion, precedence.OwnerSequence!, precedence.OwnerVersion!);
        await ObserverRegistrationConflicts.Report(GrainFactory, _observerKey.EventStore, [_observerKey.Namespace], _standbyFor);
    }

    async Task<bool> FollowDefinitionOnAnotherSequence(ReactorDefinition definition)
    {
        // Another sequence may claim the definition after this registration, or win a concurrent first registration.
        // An older registration then waits in standby; a newer one claims the definition back.
        var key = new ObserverKey(_observerKey!.ObserverId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
        var owning = await ObserverTransportSubscriptions.OnOtherSequence(storage, GrainFactory, key);
        if (owning?.IsServedByConnectedClient != true || !await _connectedClients!.IsConnected(_observerKey.ConnectionId))
        {
            return false;
        }

        var connectedClient = await _connectedClients.GetConnectedClient(_observerKey.ConnectionId);
        var precedence = ObserverRegistrationPrecedence.Evaluate(connectedClient, owning);
        switch (precedence.Outcome)
        {
            case RegistrationPrecedenceOutcome.OwnerIsNewer:
                await _observer!.UnsubscribeIfMatchesClient(_observerKey.ConnectionId);
                await EnterStandby(definition, precedence, connectedClient.Version);
                return true;

            case RegistrationPrecedenceOutcome.IncomingIsNewer:
                _subscribed = false;
                await SetDefinitionAndSubscribe(definition);
                return true;

            default:
                return false;
        }
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

        await SetDefinitionAndSubscribe(definition);
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
        logger.WaitingForWebhook(_observerKey!.ObserverId);
    }

    async Task SubscribeToObserver(ReactorKey key, ReactorDefinition definition, ConnectedClient connectedClient)
    {
        _observer = GrainFactory.GetGrain<IObserver>(key);
        var eventTypes = definition.EventTypes.Select(e => e.EventType).ToArray();
        if (definition.StartAtTail)
        {
            await _observer.StartAtTailWhenNew();
        }

        try
        {
            if (definition.GenerationDelivery == Concepts.Observation.EventGenerationDelivery.Pinned)
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
            _subscribed = true;
            _standbyFor = null;
        }
        catch (ObserverOwnedByNewerClientVersion exception)
        {
            await EnterStandby(definition, new(RegistrationPrecedenceOutcome.OwnerIsNewer, exception.OwnerSequence, exception.OwnerVersion), exception.IncomingVersion);
        }
        catch (ObserverServedByAnotherTransport)
        {
            WaitForWebhook();
        }
    }

    async Task ReplayForAllNamespaces(ReactorKey key, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            logger.AutoReplayingReactor(key.EventStore, key.ReactorId, key.EventSequenceId, @namespace);
            var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(key.ReactorId, key.EventStore, @namespace, key.EventSequenceId));
            await observer.Replay();
        }
    }

    async Task AddReplayRecommendationForAllNamespaces(ReactorKey key, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(key.EventStore, @namespace));
            await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                "Reducer definition has changed.",
                new()
                {
                    ObserverId = key.ReactorId,
                    ObserverKey = new(key.ReactorId, key.EventStore, @namespace, key.EventSequenceId),
                    ObserverType = ObserverType.Reactor,
                    Reasons = [new ReactorDefinitionChangedReplayCandidateReason()]
                });
        }
    }
}
