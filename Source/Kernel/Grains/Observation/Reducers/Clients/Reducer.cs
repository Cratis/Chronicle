// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Grains.Clients;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.Observation.States;
using Cratis.Chronicle.Grains.Recommendations;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Replaying;
using Microsoft.Extensions.Logging;
using Orleans.Placement;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducer"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Reducer"/>.
/// </remarks>
/// <param name="reducerDefinitionComparer"><see cref="IReducerDefinitionComparer"/> for comparing reducer definitions.</param>
/// <param name="localSiloDetails"><see cref="ILocalSiloDetails"/> for getting information about the silo this grain is on.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reducers)]
[PreferLocalPlacement]
public class Reducer(
    IReducerDefinitionComparer reducerDefinitionComparer,
    ILocalSiloDetails localSiloDetails,
    ILogger<Reducer> logger) : Grain<ReducerDefinition>, IReducer, INotifyClientDisconnected
{
    IObserver? _observer;
    bool _subscribed;
    ConnectedObserverKey? _observerKey;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);
        await connectedClients.SubscribeDisconnected(this.AsReference<INotifyClientDisconnected>());

        _observerKey = ConnectedObserverKey.Parse(this.GetPrimaryKeyString());
    }

    /// <inheritdoc/>
    public async Task SetDefinitionAndSubscribe(ReducerDefinition definition)
    {
        var compareResult = reducerDefinitionComparer.Compare(State, definition);

        State = definition;
        await WriteStateAsync();

        var key = ReducerKey.Parse(this.GetPrimaryKeyString());

        if (compareResult == ReducerDefinitionCompareResult.Different)
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
            _observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(key.ReducerId, key.EventStore, key.Namespace, key.EventSequenceId));

            var connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);
            var connectedClient = await connectedClients.GetConnectedClient(_observerKey!.ConnectionId!);

            await _observer.Subscribe<IReducerObserverSubscriber>(
                ObserverType.Reducer,
                definition.EventTypes.Select(_ => _.EventType).ToArray(),
                localSiloDetails.SiloAddress,
                connectedClient);

            _subscribed = true;
        }
    }

    /// <inheritdoc/>
    public void OnClientDisconnected(ConnectedClient client)
    {
        logger.ClientDisconnected(client.ConnectionId, _observerKey!.EventStore, _observerKey.ObserverId!, _observerKey!.EventSequenceId, _observerKey!.Namespace);
        var key = new ObserverKey(_observerKey.ObserverId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
        var observer = GrainFactory.GetGrain<IObserver>(key);
        observer.Unsubscribe();
        DeactivateOnIdle();
    }

    async Task AddReplayRecommendationForAllNamespaces(ReducerKey key, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(key.EventStore, @namespace));
            await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                "Reducer definition has changed.",
                new ReplayCandidateRequest
                {
                    ObserverId = key.ReducerId,
                    ObserverKey = new ObserverKey(key.ReducerId, key.EventStore, @namespace, key.EventSequenceId),
                    Reasons = [new ReducerDefinitionChangedReplayCandidateReason()]
                });
        }
    }
}
