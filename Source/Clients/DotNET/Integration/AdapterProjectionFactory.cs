// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Grains;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Schemas;
using Orleans;

namespace Aksio.Cratis.Integration
{
    /// <summary>
    /// Represents an implementation of <see cref="IAdapterProjectionFactory"/>.
    /// </summary>
    public class AdapterProjectionFactory : IAdapterProjectionFactory
    {
        readonly IEventTypes _eventTypes;
        readonly IJsonSchemaGenerator _schemaGenerator;
        readonly IClusterClient _clusterClient;
        readonly IExecutionContextManager _executionContextManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterProjectionFactory"/> class.
        /// </summary>
        /// <param name="eventTypes">The <see cref="IEventTypes"/> to use.</param>
        /// <param name="schemaGenerator">The <see cref="IJsonSchemaGenerator"/> for generating schemas.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
        /// <param name="clusterClient">Orleans <see cref="IClusterClient"/>.</param>
        public AdapterProjectionFactory(
            IEventTypes eventTypes,
            IJsonSchemaGenerator schemaGenerator,
            IExecutionContextManager executionContextManager,
            IClusterClient clusterClient)
        {
            _eventTypes = eventTypes;
            _schemaGenerator = schemaGenerator;
            _executionContextManager = executionContextManager;
            _clusterClient = clusterClient;
        }

        /// <inheritdoc/>
        public async Task<IAdapterProjectionFor<TModel>> CreateFor<TModel, TExternalModel>(IAdapterFor<TModel, TExternalModel> adapter)
        {
            // TODO: register for all tenants
            _executionContextManager.Establish("3352d47d-c154-4457-b3fb-8a2efb725113", CorrelationId.New());

            var projectionBuilder = new ProjectionBuilderFor<TModel>(adapter.Identifier.Value, _eventTypes, _schemaGenerator);
            adapter.DefineModel(projectionBuilder);
            var projectionDefinition = projectionBuilder
                .WithName($"Adapter: {adapter.GetType().Name} - {typeof(TModel).Name}")
                .Passive()
                .NotRewindable()
                .Build();

            var projections = _clusterClient.GetGrain<IProjections>(Guid.Empty);
            var pipelineDefinition = new ProjectionPipelineDefinition(
                projectionDefinition.Identifier,
                "c0c0196f-57e3-4860-9e3b-9823cf45df30", // Cratis default
                Array.Empty<ProjectionResultStoreDefinition>());

            await projections.Register(projectionDefinition, pipelineDefinition);
            var projection = _clusterClient.GetGrain<IProjection>(adapter.Identifier.Value);
            return new AdapterProjectionFor<TModel>(projection);
        }
    }
}
