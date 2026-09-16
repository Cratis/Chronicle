// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels;

/// <summary>
/// Provides read model lookups that require a registered definition.
/// </summary>
internal static class ReadModelExtensions
{
    /// <summary>
    /// Gets a registered definition rather than the unpopulated state of an unknown read model grain.
    /// </summary>
    /// <param name="readModel">The read model grain.</param>
    /// <param name="identifier">The requested identifier.</param>
    /// <returns>The registered definition.</returns>
    /// <exception cref="ReadModelNotFound">The identifier has no registered definition.</exception>
    internal static async Task<ReadModelDefinition> GetKnownDefinition(this IReadModel readModel, ReadModelIdentifier identifier)
    {
        var definition = await readModel.GetDefinition();
        if (definition?.Sink is null)
        {
            throw new ReadModelNotFound(identifier);
        }

        return definition;
    }
}
