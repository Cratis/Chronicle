// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Combines the event sequences declared for one constraint in more than one place.
/// </summary>
/// <remarks>
/// An empty declaration means every event sequence. Combining is a union in which every event sequence wins: a
/// declaration that names no sequence cannot be narrowed by another that names some, since that would silently stop
/// validating the constraint where its author asked for it to be validated everywhere.
/// </remarks>
internal static class ConstraintEventSequences
{
    /// <summary>
    /// Combine several declarations of the event sequences a constraint applies to.
    /// </summary>
    /// <param name="declarations">The declarations to combine. Each empty declaration means every event sequence.</param>
    /// <returns>The combined declaration, empty for every event sequence.</returns>
    internal static IEnumerable<EventSequenceId> Combine(IEnumerable<IEnumerable<EventSequenceId>> declarations)
    {
        var materialized = declarations.Select(_ => _.ToArray()).ToArray();
        if (materialized.Length == 0 || materialized.Any(_ => _.Length == 0))
        {
            return [];
        }

        return materialized.SelectMany(_ => _).Distinct().ToArray();
    }
}
