// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Json;

namespace Aksio.Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventSerializer"/>.
    /// </summary>
    public class EventSerializer : IEventSerializer
    {
        readonly JsonSerializerOptions _serializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSerializer"/> class.
        /// </summary>
        public EventSerializer()
        {
            _serializerOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new ConceptAsJsonConverterFactory()
                }
            };
        }

        /// <inheritdoc/>
        public object Deserialize(Type type, JsonObject json) => json.Deserialize(type, _serializerOptions)!;

        /// <inheritdoc/>
        public JsonObject Serialize(object @event) => (JsonSerializer.SerializeToNode(@event, _serializerOptions) as JsonObject)!;
    }
}
