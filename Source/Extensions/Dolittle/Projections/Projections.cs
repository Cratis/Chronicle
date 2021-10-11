// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias SDK;

using Cratis.Concepts;
using Cratis.Events.Projections;
using Cratis.Events.Projections.Json;
using Cratis.Extensions.MongoDB;
using Cratis.Types;
using Newtonsoft.Json;
using IEventTypes = SDK::Cratis.Events.IEventTypes;

namespace Cratis.Extensions.Dolittle.Projections
{
    /// <summary>
    /// Represents an implementation of <see cref="SDK::Cratis.Events.Projections.Projections"/>.
    /// </summary>
    public class Projections : SDK::Cratis.Events.Projections.Projections
    {
        readonly IMongoDBClientFactory _mongoDBClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Projections"/> class.
        /// /// </summary>
        /// <param name="eventTypes"><see cref="IEventTypes"/> to use.</param>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
        /// <param name="types"><see cref="ITypes"/> for type discovery.</param>
        public Projections(IEventTypes eventTypes, IMongoDBClientFactory mongoDBClientFactory, ITypes types) : base(eventTypes, types)
        {
            _mongoDBClientFactory = mongoDBClientFactory;
        }

        /// <inheritdoc/>
        public override void StartAll()
        {
            var projectionParser = new JsonProjectionParser();

            var converters = new JsonConverter[]
            {
                    new ConceptAsJsonConverter(),
                    new ConceptAsDictionaryJsonConverter()
            };

            foreach (var projectionDefinition in _projections)
            {
                var json = JsonConvert.SerializeObject(projectionDefinition, converters);
                var projection = projectionParser.Parse(json);
                var provider = new ProjectionEventProvider(_mongoDBClientFactory);
                var pipeline = new ProjectionPipeline(provider, projection);
                var storage = new InMemoryProjectionStorage();
                pipeline.StoreIn(storage);
                pipeline.Start();
            }
        }
    }
}
