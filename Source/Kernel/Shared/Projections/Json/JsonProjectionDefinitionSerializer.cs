// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Projections.Definitions;
using Cratis.Chronicle.Properties;
using Cratis.Json;

namespace Cratis.Chronicle.Projections.Json;

/// <summary>
/// Represents an implementation of <see cref="IJsonProjectionDefinitionSerializer"/>.
/// </summary>
public class JsonProjectionDefinitionSerializer : IJsonProjectionDefinitionSerializer
{
    readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonProjectionDefinitionSerializer"/>.
    /// </summary>
    public JsonProjectionDefinitionSerializer()
    {
        _serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
                {
                    new PropertyPathJsonConverter(),
                    new PropertyPathChildrenDefinitionDictionaryJsonConverter(),
                    new PropertyExpressionDictionaryConverter(),
                    new FromDefinitionsConverter(),
                    new JoinDefinitionsConverter(),
                    new ConceptAsJsonConverterFactory(),
                    new DateOnlyJsonConverter(),
                    new TimeOnlyJsonConverter()
                }
        };
    }

    /// <inheritdoc/>
    public JsonNode Serialize(ProjectionDefinition definition) => JsonSerializer.SerializeToNode(definition, _serializerOptions)!;

    /// <inheritdoc/>
    public ProjectionDefinition Deserialize(JsonNode json) => json.Deserialize<ProjectionDefinition>(_serializerOptions)!;
}
