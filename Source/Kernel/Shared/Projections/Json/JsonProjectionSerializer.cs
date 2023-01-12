// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Json;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Projections.Json;

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
