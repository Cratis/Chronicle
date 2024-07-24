// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Reducers"/> class.
/// </remarks>
/// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class Reducers(
    IKernel kernel,
    ILogger<Reducers> logger) : Grain, IReducers
{
    /// <inheritdoc/>
    public async Task Register(
        ConnectionId connectionId,
        IEnumerable<ReducerDefinition> definitions,
        IEnumerable<EventStoreNamespaceName> namespaces)
    {
        logger.RegisterReducers();

        var eventStoreName = (EventStoreName)this.GetPrimaryKeyString();
        var eventStore = kernel.GetEventStore(eventStoreName);

        foreach (var definition in definitions)
        {
            await eventStore.ReducerPipelineDefinitions.Register(definition);
            foreach (var @namespace in namespaces)
            {
                logger.RegisterReducer(
                    definition.ReducerId,
                    definition.EventSequenceId);

                var key = new ObserverKey(definition.ReducerId, eventStoreName, @namespace, definition.EventSequenceId);
                var reducer = GrainFactory.GetGrain<IReducer>(key);
                await reducer.Start(definition.EventTypes);
            }
        }
    }
}
