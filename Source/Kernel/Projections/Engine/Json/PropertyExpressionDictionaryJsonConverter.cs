// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;
using Newtonsoft.Json;

namespace Aksio.Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> that can convert to a dictionary of <see cref="PropertyPath"/> and expression.
    /// </summary>
    public class PropertyExpressionDictionaryJsonConverter : JsonConverter<IDictionary<PropertyPath, string>>
    {
        /// <inheritdoc/>
        public override IDictionary<PropertyPath, string>? ReadJson(JsonReader reader, Type objectType, IDictionary<PropertyPath, string>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var stringDictionary = new Dictionary<string, string>();
            serializer.Populate(reader, stringDictionary);
            return stringDictionary.ToDictionary(kvp => new PropertyPath(kvp.Key), kvp => kvp.Value);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, IDictionary<PropertyPath, string>? value, JsonSerializer serializer)
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
