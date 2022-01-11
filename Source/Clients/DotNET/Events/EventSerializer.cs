// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;
using Newtonsoft.Json;

namespace Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventSerializer"/>.
    /// </summary>
    public class EventSerializer : IEventSerializer
    {
        readonly JsonSerializer _serializer;

        public EventSerializer()
        {
            _serializer = new();
            _serializer.Converters.Add(new ConceptAsJsonConverter());
            _serializer.Converters.Add(new ConceptAsDictionaryJsonConverter());
        }

        /// <inheritdoc/>
        public object Deserialize(Type type, string json) => _serializer.Deserialize(new StringReader(json), type)!;

        /// <inheritdoc/>
        public string Serialize(object @event)
        {
            var stringWriter = new StringWriter();
            _serializer.Serialize(stringWriter, @event);
            return stringWriter.ToString();
        }
    }
}
