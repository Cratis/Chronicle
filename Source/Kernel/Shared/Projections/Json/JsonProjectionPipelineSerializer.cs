// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Projections.Definitions;

namespace Cratis.Chronicle.Projections.Json;

/// <summary>
/// Represents an implementation of <see cref="IJsonProjectionSerializer"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JsonProjectionPipelineSerializer"/>.
/// </remarks>
/// <param name="serializerOptions">The common <see creF="JsonSerializerOptions"/>.</param>
public class JsonProjectionPipelineSerializer(JsonSerializerOptions serializerOptions) : IJsonProjectionPipelineSerializer
{
    /// <inheritdoc/>
    public string Serialize(ProjectionPipelineDefinition definition) => JsonSerializer.Serialize(definition, serializerOptions);

    /// <inheritdoc/>
    public ProjectionPipelineDefinition Deserialize(string json) => JsonSerializer.Deserialize<ProjectionPipelineDefinition>(json, serializerOptions)!;
}
