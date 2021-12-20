// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Definitions;
using Newtonsoft.Json;

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> that can convert a from definition based on key/value of event type identifier to from definition.
    /// </summary>
    public class FromDefinitionsConverter : JsonConverter<IDictionary<EventType, FromDefinition>>
    {
        /// <inheritdoc/>
        public override IDictionary<EventType, FromDefinition>? ReadJson(JsonReader reader, Type objectType, IDictionary<EventType, FromDefinition>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var stringDictionary = new Dictionary<string, FromDefinition>();
            serializer.Populate(reader, stringDictionary);
            return stringDictionary.ToDictionary(kvp => new EventType(kvp.Key, 1), kvp => kvp.Value);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, IDictionary<EventType, FromDefinition>? value, JsonSerializer serializer)
        {
            if (value is null) return;
            writer.WriteStartObject();
            foreach (var (key, children) in value)
            {
                writer.WritePropertyName(key.Id.Value.ToString());
                serializer.Serialize(writer, children);
            }

            writer.WriteEndObject();
        }
    }
}
