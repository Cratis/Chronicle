// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Grains.Namespaces;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducersManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Reducers"/> class.
/// </remarks>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.ReducersManager)]
public class ReducersManager(ILogger<ReducersManager> logger) : Grain<ReducersManagerState>, IReducersManager
{
    EventStoreName _eventStoreName = EventStoreName.NotSet;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _eventStoreName = this.GetPrimaryKeyString();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register(ConnectionId connectionId, IEnumerable<ReducerDefinition> definitions)
    {
        logger.RegisterReducers();
        await SetDefinitionForAllProjectionsInAllNamespaces(connectionId);
    }

    async Task SetDefinitionForAllProjectionsInAllNamespaces(ConnectionId connectionId)
    {
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        foreach (var namespaceName in namespaces)
        {
            await SetDefinitionForAllProjectionsForNamespace(connectionId, namespaceName);
        }
    }

    async Task SetDefinitionForAllProjectionsForNamespace(ConnectionId connectionId, EventStoreNamespaceName namespaceName)
    {
        foreach (var reducerDefinition in State.Reducers)
        {
            var key = new ConnectedObserverKey(reducerDefinition.Identifier, _eventStoreName, namespaceName, reducerDefinition.EventSequenceId, connectionId);
            var projection = GrainFactory.GetGrain<IReducer>(key);
            await projection.SetDefinitionAndSubscribe(reducerDefinition);
        }
    }
}
