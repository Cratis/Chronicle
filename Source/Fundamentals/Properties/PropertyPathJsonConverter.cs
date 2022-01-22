// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aksio.Cratis.Properties
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> for converting <see cref="PropertyPath"/>.
    /// </summary>
    public class PropertyPathJsonConverter : JsonConverter<PropertyPath>
    {
        /// <inheritdoc/>
        public override PropertyPath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetString() ?? string.Empty);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, PropertyPath value, JsonSerializerOptions options) => writer.WriteStringValue(value.Path);
    }
}
