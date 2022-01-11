// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias SDK;

using System.Reactive.Linq;
using Cratis.Concepts;
using Cratis.Events.Projections;
using Cratis.Events.Projections.Definitions;
using Cratis.Events.Projections.Json;
using Cratis.Events.Projections.MongoDB;
using Cratis.Execution;
using Cratis.Schemas;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using IEventTypes = SDK::Cratis.Events.IEventTypes;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="SDK::Cratis.Events.Projections.IProjectionsRegistrar"/>.
    /// </summary>
    [Singleton]
    public class ProjectionsRegistrar : SDK::Cratis.Events.Projections.IProjectionsRegistrar
    {
        readonly IJsonProjectionSerializer _projectionSerializer;
        readonly IServiceProvider _serviceProvider;
        readonly IEnumerable<ProjectionDefinition> _projections;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projections"/> class.
        /// /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for serialization of projection definitions.</param>
        /// <param name="projectionsReady"><see cref="ProjectionsReady"/> observable for being notified when projections are ready.</param>
        /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> used for getting instances from the IoC.</param>
        public ProjectionsRegistrar(
            IEventTypes eventTypes,
            ITypes types,
            IJsonProjectionSerializer projectionSerializer,
            ProjectionsReady projectionsReady,
            IJsonSchemaGenerator schemaGenerator,
            IServiceProvider serviceProvider)
        {
            _projections = SDK::Cratis.Events.Projections.ProjectionsRegistrar.FindAllProjectionDefinitions(eventTypes, types, schemaGenerator);
            _projectionSerializer = projectionSerializer;
            _serviceProvider = serviceProvider;
            projectionsReady.IsReady.Subscribe(async _ => await ActualStartAll());
        }

        /// <inheritdoc/>
        public Task StartAll() => Task.CompletedTask;

        async Task ActualStartAll()
        {
            var projections = _serviceProvider.GetService<IProjections>()!;

            var converters = new JsonConverter[]
            {
                new ConceptAsJsonConverter(),
                new ConceptAsDictionaryJsonConverter()
            };

            foreach (var projectionDefinition in _projections)
            {
                var json = JsonConvert.SerializeObject(projectionDefinition, converters);
                var parsed = _projectionSerializer.Deserialize(json);
                var pipelineDefinition = new ProjectionPipelineDefinition(
                    parsed.Identifier,
                    ProjectionEventProvider.ProjectionEventProviderTypeId,
                    new[] {
                        new ProjectionResultStoreDefinition(
                            "12358239-a120-4392-96d4-2b48271b904c",
                            MongoDBProjectionResultStore.ProjectionResultStoreTypeId)
                    });

                await projections.Register(parsed, pipelineDefinition);
            }

            await projections.Start();
        }
    }
}
