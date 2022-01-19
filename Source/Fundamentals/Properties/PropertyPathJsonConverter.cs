// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Aksio.Cratis.Properties
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> for converting <see cref="PropertyPath"/>.
    /// </summary>
    public class PropertyPathJsonConverter : JsonConverter<PropertyPath>
    {
        /// <inheritdoc/>
        public override PropertyPath? ReadJson(JsonReader reader, Type objectType, PropertyPath? existingValue, bool hasExistingValue, JsonSerializer serializer) => new(reader.Value?.ToString() ?? string.Empty);

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, PropertyPath? value, JsonSerializer serializer) => writer.WriteValue(value?.Path ?? string.Empty);
    }
}
