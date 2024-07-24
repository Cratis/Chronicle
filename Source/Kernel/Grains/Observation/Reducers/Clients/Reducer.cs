// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Grains.Clients;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Utilities;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducer"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Reducer"/>.
/// </remarks>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reducers)]
public class Reducer(ILogger<Reducer> logger) : Grain<ReducerDefinition>, IReducer, INotifyClientDisconnected
{
    readonly ObserverManager<INotifyReducerDefinitionsChanged> _definitionObservers = new(TimeSpan.FromMinutes(1), logger);
    IObserver? _observer;
    bool _subscribed;
    ConnectedObserverKey? _observerKey;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _observerKey = ConnectedObserverKey.Parse(this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetDefinitionAndSubscribe(ReducerDefinition definition) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Task SubscribeDefinitionsChanged(INotifyReducerDefinitionsChanged subscriber) => throw new NotImplementedException();


    /// <inheritdoc/>
    public void OnClientDisconnected(ConnectedClient client)
    {
        logger.ClientDisconnected(client.ConnectionId, _observerKey!.EventStore, _observerKey.ObserverId!, _observerKey!.EventSequenceId, _observerKey!.Namespace);
        var key = new ObserverKey(_observerKey.ObserverId, _observerKey.EventStore, _observerKey.Namespace, _observerKey.EventSequenceId);
        var observer = GrainFactory.GetGrain<IObserver>(key);
        observer.Unsubscribe();
    }
}
