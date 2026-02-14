// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Grains.Clients;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.Observation.States;
using Cratis.Chronicle.Grains.Recommendations;
using Microsoft.Extensions.Logging;
using Orleans.Placement;
using Orleans.Providers;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReactor"/>.
/// </summary>
/// <param name="reactorDefinitionComparer"><see cref="IReactorDefinitionComparer"/> for comparing reactor definitions.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reactors)]
[PreferLocalPlacement]
public class Reactor(
    IReactorDefinitionComparer reactorDefinitionComparer,
    ILocalSiloDetails localSiloDetails,
    ILogger<Reactor> logger) : Grain<ReactorDefinition>, IReactor
{
    IConnectedClients? _connectedClients;
    ConnectedObserverKey? _observerKey;
    IObserver? _observer;
    bool _subscribed;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);

        _observerKey = ConnectedObserverKey.Parse(this.GetPrimaryKeyString());

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SetDefinitionAndSubscribe(ReactorDefinition definition)
    {
        logger.Starting(_observerKey!.EventStore, _observerKey.ObserverId!, _observerKey!.EventSequenceId, _observerKey!.Namespace);
        var key = new ReactorKey(_observerKey.ObserverId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
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
            var namespaceNames = await GrainFactory.GetGrain<INamespaces>(key.EventStore).GetAll();
            await AddReplayRecommendationForAllNamespaces(key, namespaceNames);
        }

        if (!_subscribed)
        {
            _observer = GrainFactory.GetGrain<IObserver>(key);
            var connectedClient = await _connectedClients!.GetConnectedClient(_observerKey.ConnectionId!);
            var eventTypes = definition.EventTypes.Select(e => e.EventType).ToArray();
            await _observer.Subscribe<IReactorObserverSubscriber>(
                ObserverType.Reactor,
                eventTypes,
                localSiloDetails.SiloAddress,
                connectedClient,
                definition.IsReplayable);
            _subscribed = true;
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
        await observer.Unsubscribe();

        _subscribed = false;
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
