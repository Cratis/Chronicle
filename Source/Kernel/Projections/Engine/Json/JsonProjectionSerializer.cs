// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Json;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents an implementation of <see cref="IJsonProjectionSerializer"/>.
    /// </summary>
    public class JsonProjectionSerializer : IJsonProjectionSerializer
    {
        readonly JsonSerializerOptions _serializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonProjectionSerializer"/>.
        /// </summary>
        public JsonProjectionSerializer()
        {
            _serializerOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new PropertyPathJsonConverter(),
                    new ConceptAsJsonConverterFactory()
                }
            };
        }

        /// <inheritdoc/>
        public string Serialize(ProjectionDefinition definition) => JsonSerializer.Serialize(definition, _serializerOptions);

        /// <inheritdoc/>
        public ProjectionDefinition Deserialize(string json) => JsonSerializer.Deserialize<ProjectionDefinition>(json, _serializerOptions)!;
    }
}
