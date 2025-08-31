// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Projections;

/// <summary>
/// Converter methods for working with <see cref="ProjectionDefinition"/> converting to and from SQL representations.
/// </summary>
public static class ProjectionDefinitionConverters
{
    /// <summary>
    /// Convert to a <see cref="Projection">SQL</see> representation.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to convert.</param>
    /// <returns>Converted <see cref="Projection"/>.</returns>
    public static Projection ToSql(this ProjectionDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            Owner = definition.Owner,
            ReadModelName = definition.ReadModel,
            ReadModelGeneration = ReadModelGeneration.First,
            SinkType = definition.Sink.TypeId,
            SinkConfigurationId = definition.Sink.ConfigurationId,
            Definitions = new Dictionary<uint, string>
            {
                { ProjectionGeneration.First, JsonSerializer.Serialize(definition) }
            }
        };

    /// <summary>
    /// Convert to <see cref="ProjectionDefinition"/> from <see cref="Projection"/>.
    /// </summary>
    /// <param name="projection"><see cref="Projection"/> to convert from.</param>
    /// <returns>Converted <see cref="ProjectionDefinition"/>.</returns>
    public static ProjectionDefinition ToKernel(this Projection projection) =>
        JsonSerializer.Deserialize<ProjectionDefinition>(projection.Definitions[ProjectionGeneration.First])!;
}
