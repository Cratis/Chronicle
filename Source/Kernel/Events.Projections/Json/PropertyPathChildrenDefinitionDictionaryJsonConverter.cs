// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Definitions;
using Cratis.Properties;
using Newtonsoft.Json;

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> that can convert to a dictionary of <see cref="PropertyPath"/> and <see cref="ChildrenDefinition"/>.
    /// </summary>
    public class PropertyPathChildrenDefinitionDictionaryJsonConverter : JsonConverter<IDictionary<PropertyPath, ChildrenDefinition>>
    {
        /// <inheritdoc/>
        public override IDictionary<PropertyPath, ChildrenDefinition>? ReadJson(JsonReader reader, Type objectType, IDictionary<PropertyPath, ChildrenDefinition>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var stringDictionary = new Dictionary<string, ChildrenDefinition>();
            serializer.Populate(reader, stringDictionary);
            return stringDictionary.ToDictionary(kvp => new PropertyPath(kvp.Key), kvp => kvp.Value);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, IDictionary<PropertyPath, ChildrenDefinition>? value, JsonSerializer serializer)
        {
            if (value is null) return;
            writer.WriteStartObject();
            foreach (var (key, children) in value)
            {
                writer.WritePropertyName(key.ToString());
                serializer.Serialize(writer, children);
            }

            writer.WriteEndObject();
        }
    }
}
