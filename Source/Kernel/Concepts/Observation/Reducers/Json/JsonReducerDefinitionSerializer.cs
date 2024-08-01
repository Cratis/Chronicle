// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Properties;
using Cratis.Json;

namespace Cratis.Chronicle.Concepts.Observation.Reducers.Json;

/// <summary>
/// Represents an implementation of <see cref="IJsonReducerDefinitionSerializer"/>.
/// </summary>
public class JsonReducerDefinitionSerializer : IJsonReducerDefinitionSerializer
{
    readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonReducerDefinitionSerializer"/>.
    /// </summary>
    public JsonReducerDefinitionSerializer()
    {
        _serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
                {
                    new PropertyPathJsonConverter(),
                    new ConceptAsJsonConverterFactory(),
                    new DateOnlyJsonConverter(),
                    new TimeOnlyJsonConverter()
                }
        };
    }

    /// <inheritdoc/>
    public JsonNode Serialize(ReducerDefinition definition) => JsonSerializer.SerializeToNode(definition, _serializerOptions)!;

    /// <inheritdoc/>
    public ReducerDefinition Deserialize(JsonNode json) => json.Deserialize<ReducerDefinition>(_serializerOptions)!;
}
