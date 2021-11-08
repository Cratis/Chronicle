// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> for converting <see cref="Property"/>.
    /// </summary>
    public class PropertyJsonConverter : JsonConverter<Property>
    {
        /// <inheritdoc/>
        public override Property? ReadJson(JsonReader reader, Type objectType, Property? existingValue, bool hasExistingValue, JsonSerializer serializer) => new(reader.Value?.ToString() ?? string.Empty);

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, Property? value, JsonSerializer serializer) => writer.WriteValue(value?.Path ?? string.Empty);
    }
}
