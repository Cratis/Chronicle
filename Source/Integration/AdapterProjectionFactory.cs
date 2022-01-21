// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Events.Projections;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Projections.Grains;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterProjectionFactory"/> class.
        /// </summary>
        /// <param name="eventTypes">The <see cref="IEventTypes"/> to use.</param>
        /// <param name="schemaGenerator">The <see cref="IJsonSchemaGenerator"/> for generating schemas.</param>
        /// <param name="clusterClient">Orleans <see cref="IClusterClient"/>.</param>
        public AdapterProjectionFactory(
            IEventTypes eventTypes,
            IJsonSchemaGenerator schemaGenerator,
            IClusterClient clusterClient)
        {
            _eventTypes = eventTypes;
            _schemaGenerator = schemaGenerator;
            _clusterClient = clusterClient;
        }

        /// <inheritdoc/>
        public async Task<IAdapterProjectionFor<TModel>> CreateFor<TModel, TExternalModel>(IAdapterFor<TModel, TExternalModel> adapter)
        {
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
                new[]
                {
                        new ProjectionResultStoreDefinition(
                            "dc8698c9-51a1-4217-aa02-d4803b25f8e1",
                            "22202c41-2be1-4547-9c00-f0b1f797fd75") // MongoDB
                });

            await projections.Register(projectionDefinition, pipelineDefinition);

            // Get AdapterID
            // Register Projection with definition with kernel
            // Get projection grain
            return new AdapterProjectionFor<TModel>();
        }
    }
}
