// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Extension methods for projection definitions.
/// </summary>
public static class ProjectionDefinitionExtensions
{
    /// <summary>
    /// Determines whether a non-kernel projection addresses an unsupported event sequence.
    /// </summary>
    /// <param name="definition">The projection definition.</param>
    /// <returns>Whether the definition addresses an unsupported event sequence.</returns>
    public static bool UsesUnsupportedEventSequence(this ProjectionDefinition definition) =>
        definition.Owner != ProjectionOwner.Kernel && definition.EventSequenceId.IsSystemSequence();
}
