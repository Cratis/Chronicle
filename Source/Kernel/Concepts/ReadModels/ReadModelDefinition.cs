// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NJsonSchema;

namespace Cratis.Chronicle.Concepts.ReadModels;

/// <summary>
/// Represents the model used by a projection to project to.
/// </summary>
/// <param name="Name">Name of the model.</param>
/// <param name="Owner">The owner of the read model.</param>
/// <param name="Schemas">The <see cref="JsonSchema"/> for the model.</param>
[GenerateSerializer]
[Alias(nameof(ReadModelDefinition))]
public record ReadModelDefinition(ReadModelName Name, ReadModelOwner Owner, IDictionary<ReadModelGeneration, JsonSchema> Schemas)
{
    /// <summary>
    /// Gets the latest generation of the read model.
    /// </summary>
    public ReadModelGeneration LatestGeneration => Schemas.Keys.Max() ?? ReadModelGeneration.Unspecified;

    /// <summary>
    /// Gets the schema for the latest generation of the read model.
    /// </summary>
    /// <returns>The <see cref="JsonSchema"/> for the latest generation.</returns>
    /// <exception cref="MissingSchemaForReadModel">Thrown when the read model does not have a schema defined for the latest generation.</exception>
    public JsonSchema GetSchemaForLatestGeneration()
    {
        if (Schemas.Count == 0)
        {
            throw new MissingSchemaForReadModel(Name);
        }

        var latestGeneration = Schemas.Keys.Max()!;
        return Schemas[latestGeneration];
    }
}
