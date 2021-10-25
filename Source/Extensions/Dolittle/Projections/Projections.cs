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
        readonly JsonProjectionParser _projectionParser;
        readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projections"/> class.
        /// /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        /// <param name="projectionParser"><see cref="JsonProjectionParser"/> for parsing JSON projection definitions.</param>
        /// <param name="projectionsReady"><see cref="ProjectionsReady"/> observable for being notified when projections are ready.</param>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
        public Projections(
            IEventTypes eventTypes,
            IMongoDBClientFactory mongoDBClientFactory,
            ITypes types,
            JsonProjectionParser projectionParser,
            ProjectionsReady projectionsReady,
            ILoggerFactory loggerFactory) : base(eventTypes, types)
        {
            _mongoDBClientFactory = mongoDBClientFactory;
            _projectionParser = projectionParser;
            _loggerFactory = loggerFactory;
            projectionsReady.IsReady.Subscribe(_ => ActualStartAll());
        }

        /// <inheritdoc/>
        public override void StartAll()
        {
        }

        void ActualStartAll()
        {
            var converters = new JsonConverter[]
            {
                new ConceptAsJsonConverter(),
                new ConceptAsDictionaryJsonConverter()
            };

            foreach (var projectionDefinition in _projections)
            {
                var json = JsonConvert.SerializeObject(projectionDefinition, converters);
                var projection = _projectionParser.Parse(json);
                var projectionPositions = new ProjectionPositions(_mongoDBClientFactory);
                var provider = new ProjectionEventProvider(_mongoDBClientFactory, projectionPositions, _loggerFactory.CreateLogger<ProjectionEventProvider>());
                var pipeline = new ProjectionPipeline(provider, projection, _loggerFactory.CreateLogger<ProjectionPipeline>());
                //var storage = new InMemoryProjectionStorage();
                var storage = new MongoDBProjectionStorage(_mongoDBClientFactory);
                pipeline.StoreIn(storage);
                pipeline.Start();
            }
        }
    }
}
