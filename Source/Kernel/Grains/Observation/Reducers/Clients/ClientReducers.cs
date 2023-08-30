// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Kernel.Engines.Observation.Reducers;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Observation.Reducers;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientReducers"/>.
/// </summary>
public class ClientReducers : Grain, IClientReducers
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IReducerPipelineDefinitions> _reducerPipelineDefinitionsProvider;
    readonly ILogger<ClientReducers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientReducers"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="reducerPipelineDefinitionsProvider">Provider for <see cref="IReducerPipelineDefinitions"/> for registering pipeline definitions.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientReducers(
        IExecutionContextManager executionContextManager,
        ProviderFor<IReducerPipelineDefinitions> reducerPipelineDefinitionsProvider,
        ILogger<ClientReducers> logger)
    {
        _executionContextManager = executionContextManager;
        _reducerPipelineDefinitionsProvider = reducerPipelineDefinitionsProvider;
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

        foreach (var definition in definitions)
        {
            foreach (var tenantId in tenants)
            {
                _logger.RegisterReducer(
                    definition.ReducerId,
                    definition.Name,
                    definition.EventSequenceId);

                _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);
                await _reducerPipelineDefinitionsProvider().Register(definition);

                var key = new ObserverKey(microserviceId, tenantId, definition.EventSequenceId);
                var reducer = GrainFactory.GetGrain<IClientReducer>(definition.ReducerId, key);
                await reducer.Start(definition.Name, connectionId, definition.EventTypes);
            }
        }
    }
}
