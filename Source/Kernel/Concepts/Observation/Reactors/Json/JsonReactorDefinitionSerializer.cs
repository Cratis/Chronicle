// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Properties;
using Cratis.Json;

namespace Cratis.Chronicle.Concepts.Observation.Reactors.Json;

/// <summary>
/// Represents an implementation of <see cref="IJsonReactorDefinitionSerializer"/>.
/// </summary>
public class JsonReactorDefinitionSerializer : IJsonReactorDefinitionSerializer
{
    readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonReactorDefinitionSerializer"/>.
    /// </summary>
    public JsonReactorDefinitionSerializer()
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
    public JsonNode Serialize(ReactorDefinition definition) => JsonSerializer.SerializeToNode(definition, _serializerOptions)!;

    /// <inheritdoc/>
    public ReactorDefinition Deserialize(JsonNode json) => json.Deserialize<ReactorDefinition>(_serializerOptions)!;
}
