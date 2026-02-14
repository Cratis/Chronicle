// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;
using Microsoft.Extensions.Logging;
using Orleans.Placement;
using Orleans.Providers;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

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
    ILogger<Reducer> logger) : Grain<ReducerDefinition>, IReducer
{
    IObserver? _observer;
    bool _subscribed;
    ConnectedObserverKey? _observerKey;
    IConnectedClients? _connectedClients;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _observerKey = ConnectedObserverKey.Parse(this.GetPrimaryKeyString());
        _connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SetDefinitionAndSubscribe(ReducerDefinition definition)
    {
        var key = ReducerKey.Parse(this.GetPrimaryKeyString());
        var compareResult = await reducerDefinitionComparer.Compare(key, State, definition);

        State = definition;
        await WriteStateAsync();

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

        if (!_subscribed && definition.IsActive)
        {
            _observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(key.ReducerId, key.EventStore, key.Namespace, key.EventSequenceId));

            var connectedClient = await _connectedClients!.GetConnectedClient(_observerKey!.ConnectionId!);

            await _observer.Subscribe<IReducerObserverSubscriber>(
                ObserverType.Reducer,
                definition.EventTypes.Select(_ => _.EventType).ToArray(),
                localSiloDetails.SiloAddress,
                connectedClient);

            _subscribed = true;
        }
        else if (_subscribed && !definition.IsActive)
        {
            await _observer!.Unsubscribe();
            _subscribed = false;
        }
    }

    /// <inheritdoc/>
    public async Task Unsubscribe()
    {
        if (_subscribed)
        {
            logger.ClientDisconnected(
                _observerKey!.ConnectionId,
                _observerKey!.EventStore,
                _observerKey.ObserverId!,
                _observerKey!.EventSequenceId,
                _observerKey!.Namespace);

            var key = new ObserverKey(_observerKey.ObserverId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
            var observer = GrainFactory.GetGrain<IObserver>(key);
            DeactivateOnIdle();
            await observer.Unsubscribe();
            _subscribed = false;
        }
    }

    async Task AddReplayRecommendationForAllNamespaces(ReducerKey key, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(key.EventStore, @namespace));
            await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                "Reducer definition has changed.",
                new()
                {
                    ObserverId = key.ReducerId,
                    ObserverKey = new(key.ReducerId, key.EventStore, @namespace, key.EventSequenceId),
                    ObserverType = ObserverType.Reducer,
                    Reasons = [new ReducerDefinitionChangedReplayCandidateReason()]
                });
        }
    }
}
