// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Projections.Json;

/// <summary>
/// Represents an implementation of <see cref="IJsonProjectionSerializer"/>.
/// </summary>
public class JsonProjectionPipelineSerializer : IJsonProjectionPipelineSerializer
{
    readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonProjectionPipelineSerializer"/>.
    /// </summary>
    /// <param name="serializerOptions">The common <see creF="JsonSerializerOptions"/>.</param>
    public JsonProjectionPipelineSerializer(JsonSerializerOptions serializerOptions)
    {
        _serializerOptions = serializerOptions;
    }

    /// <inheritdoc/>
    public string Serialize(ProjectionPipelineDefinition definition) => JsonSerializer.Serialize(definition, _serializerOptions);

    /// <inheritdoc/>
    public ProjectionPipelineDefinition Deserialize(string json) => JsonSerializer.Deserialize<ProjectionPipelineDefinition>(json, _serializerOptions)!;
}
