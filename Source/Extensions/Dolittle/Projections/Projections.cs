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
using Cratis.Extensions.MongoDB;
using Cratis.Types;
using Newtonsoft.Json;
using IEventTypes = SDK::Cratis.Events.IEventTypes;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="SDK::Cratis.Events.Projections.Projections"/>.
    /// </summary>
    [Singleton]
    public class Projections : SDK::Cratis.Events.Projections.Projections
    {
        readonly IMongoDBClientFactory _mongoDBClientFactory;
        readonly IJsonProjectionSerializer _projectionSerializer;
        readonly IProjections _projectionsCoordinator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projections"/> class.
        /// /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for serialization of projection definitions.</param>
        /// <param name="projectionsReady"><see cref="ProjectionsReady"/> observable for being notified when projections are ready.</param>
        /// <param name="projections"><see cref="IProjections"/> that supervises the projections.</param>
        public Projections(
            IEventTypes eventTypes,
            IMongoDBClientFactory mongoDBClientFactory,
            ITypes types,
            IJsonProjectionSerializer projectionSerializer,
            ProjectionsReady projectionsReady,
            IProjections projections) : base(eventTypes, types)
        {
            _mongoDBClientFactory = mongoDBClientFactory;
            _projectionSerializer = projectionSerializer;
            _projectionsCoordinator = projections;
            projectionsReady.IsReady.Subscribe(async _ => await ActualStartAll());
        }

        /// <inheritdoc/>
        public override void StartAll()
        {
        }

        async Task ActualStartAll()
        {
            var converters = new JsonConverter[]
            {
                new ConceptAsJsonConverter(),
                new ConceptAsDictionaryJsonConverter()
            };

            var projectionDefinitions = new MongoDBProjectionDefinitionsStorage(_mongoDBClientFactory, _projectionSerializer);
            var definitions = await projectionDefinitions.GetAll();
            var definitionsAsJson = definitions.ToDictionary(_ => _.Identifier, _ => _projectionSerializer.Serialize(_));
            var newDefinitionsAsJson = new Dictionary<ProjectionId, string>();
            var newDefinitions = new Dictionary<ProjectionId, ProjectionDefinition>();

            var pipelines = new Dictionary<ProjectionId, IProjectionPipeline>();

            foreach (var projectionDefinition in _projections)
            {
                var json = JsonConvert.SerializeObject(projectionDefinition, converters);
                var parsed = _projectionSerializer.Deserialize(json);

                newDefinitionsAsJson[parsed.Identifier] = _projectionSerializer.Serialize(parsed);
                newDefinitions[parsed.Identifier] = parsed;

                var pipelineDefinition = new ProjectionPipelineDefinition(
                    parsed.Identifier,
                    ProjectionEventProvider.ProjectionEventProviderTypeId,
                    new[] {
                        new ProjectionResultStoreDefinition(
                            "12358239-a120-4392-96d4-2b48271b904c",
                            MongoDBProjectionResultStore.ProjectionResultStoreTypeId)
                    });

                await _projectionsCoordinator.Register(parsed, pipelineDefinition);
            }

            _projectionsCoordinator.Start();
        }
    }
}
