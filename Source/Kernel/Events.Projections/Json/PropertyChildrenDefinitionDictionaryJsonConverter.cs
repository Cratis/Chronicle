// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Cratis.Events.Projections.Definitions;
using Cratis.Properties;

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> that can convert to a dictionary of <see cref="Property"/> and <see cref="ChildrenDefinition"/>.
    /// </summary>
    public class PropertyChildrenDefinitionDictionaryJsonConverter : JsonConverter<IDictionary<Property, ChildrenDefinition>>
    {
        /// <inheritdoc/>
        public override IDictionary<Property, ChildrenDefinition>? ReadJson(JsonReader reader, Type objectType, IDictionary<Property, ChildrenDefinition>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var stringDictionary = new Dictionary<string, ChildrenDefinition>();
            serializer.Populate(reader, stringDictionary);
            return stringDictionary.ToDictionary(kvp => new Property(kvp.Key), kvp => kvp.Value);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, IDictionary<Property, ChildrenDefinition>? value, JsonSerializer serializer) => writer.WriteValue(value!.ToDictionary(kvp => kvp.Key.Path, kvp => kvp.Value));
    }

}
