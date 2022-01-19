// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Aksio.Cratis.Concepts
{
    /// <summary>
    /// Represents a <see cref="JsonConverter"/> for <see cref="ConceptAs{T}"/>.
    /// </summary>
    public class ConceptAsJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType) => objectType.IsConcept();

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) => ConceptFactory.CreateConceptInstance(objectType, reader.Value);

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) => writer.WriteValue(value?.GetConceptValue());
    }
}
