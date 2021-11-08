// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> that can convert to a dictionary of <see cref="Property"/> and expression.
    /// </summary>
    public class PropertyExpressionDictionaryJsonConverter : JsonConverter<IDictionary<Property, string>>
    {
        /// <inheritdoc/>
        public override IDictionary<Property, string>? ReadJson(JsonReader reader, Type objectType, IDictionary<Property, string>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var stringDictionary = new Dictionary<string, string>();
            serializer.Populate(reader, stringDictionary);
            return stringDictionary.ToDictionary(kvp => new Property(kvp.Key), kvp => kvp.Value);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, IDictionary<Property, string>? value, JsonSerializer serializer) => writer.WriteValue(value!.ToDictionary(kvp => kvp.Key.Path, kvp => kvp.Value));
    }

}
