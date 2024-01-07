// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Observation.Reducers;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientReducers"/>.
/// </summary>
public class ClientReducers : Grain, IClientReducers
{
    readonly IKernel _kernel;
    readonly ILogger<ClientReducers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientReducers"/> class.
    /// </summary>
    /// <param name="kernel"><see cref="IKernel"/> for accessing global artifacts.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientReducers(
        IKernel kernel,
        ILogger<ClientReducers> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Register(
        ConnectionId connectionId,
        IEnumerable<ReducerDefinition> definitions,
        IEnumerable<TenantId> tenants)
    {
        _logger.RegisterReducers();

        var microserviceId = (MicroserviceId)this.GetPrimaryKey();

        var eventStore = _kernel.GetEventStore((string)microserviceId);

        foreach (var definition in definitions)
        {
            await eventStore.ReducerPipelineDefinitions.Register(definition);
            foreach (var tenantId in tenants)
            {
                _logger.RegisterReducer(
                    definition.ReducerId,
                    definition.Name,
                    definition.EventSequenceId);

                var key = new ObserverKey(microserviceId, tenantId, definition.EventSequenceId);
                var reducer = GrainFactory.GetGrain<IClientReducer>(definition.ReducerId, key);
                await reducer.Start(definition.Name, connectionId, definition.EventTypes);
            }
        }
    }
}
