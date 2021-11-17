// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias SDK;

using System.Reactive.Linq;
using Cratis.Concepts;
using Cratis.Events.Projections;
using Cratis.Events.Projections.Json;
using Cratis.Events.Projections.MongoDB;
using Cratis.Execution;
using Cratis.Extensions.MongoDB;
using Cratis.Types;
using Microsoft.Extensions.Logging;
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
        readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projections"/> class.
        /// /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for serialization of projection definitions.</param>
        /// <param name="projectionsReady"><see cref="ProjectionsReady"/> observable for being notified when projections are ready.</param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
        public Projections(
            IEventTypes eventTypes,
            IMongoDBClientFactory mongoDBClientFactory,
            ITypes types,
            IJsonProjectionSerializer projectionSerializer,
            ProjectionsReady projectionsReady,
            ILoggerFactory loggerFactory) : base(eventTypes, types)
        {
            _mongoDBClientFactory = mongoDBClientFactory;
            _projectionSerializer = projectionSerializer;
            _loggerFactory = loggerFactory;
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

            foreach (var projectionDefinition in _projections)
            {
                var json = JsonConvert.SerializeObject(projectionDefinition, converters);
                var parsed = _projectionSerializer.Deserialize(json);

                var projectionDefinitions = new MongoDBProjectionDefinitions(_mongoDBClientFactory, _projectionSerializer);
                await projectionDefinitions.Save(parsed);

                var projection = _projectionSerializer.CreateFrom(parsed);
                var projectionPositions = new ProjectionPositions(_mongoDBClientFactory);

                var eventStore = new EventStore.EventStore(_mongoDBClientFactory, _loggerFactory);
                var provider = new ProjectionEventProvider(eventStore, projectionPositions, _loggerFactory.CreateLogger<ProjectionEventProvider>());
                var changesetStorage = new MongoDBChangesetStorage(_mongoDBClientFactory);
                var pipeline = new ProjectionPipeline(provider, projection, changesetStorage, _loggerFactory.CreateLogger<ProjectionPipeline>());
                //var storage = new InMemoryProjectionStorage();
                var resultStore = new MongoDBProjectionResultStore(_mongoDBClientFactory);
                pipeline.StoreIn(resultStore);
                pipeline.Start();
            }
        }
    }
}
