// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Concepts.SystemJson;

namespace Cratis.Events
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
        public object Deserialize(Type type, string json) => JsonSerializer.Deserialize(json, type, _serializerOptions)!;

        /// <inheritdoc/>
        public string Serialize(object @event) => JsonSerializer.Serialize(@event, _serializerOptions);
    }
}
