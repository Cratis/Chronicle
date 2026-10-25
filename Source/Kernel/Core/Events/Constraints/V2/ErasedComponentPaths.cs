// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2;

/// <summary>
/// Matches erased protected boundaries to selected components, including containers and array elements.
/// </summary>
internal static class ErasedComponentPaths
{
    /// <summary>
    /// Determines whether any selected component overlaps an erased protected boundary.
    /// </summary>
    /// <param name="definition">The selected event properties.</param>
    /// <param name="statuses">The release statuses keyed by protected boundary path.</param>
    /// <returns>Whether claiming or removing this tuple must be skipped.</returns>
    internal static bool Affects(UniqueConstraintEventDefinition definition, IReadOnlyDictionary<string, ProtectedValueReleaseStatus> statuses) =>
        statuses.Any(status => status.Value == ProtectedValueReleaseStatus.Erased &&
            definition.Properties.Any(property => IsWithin(property, status.Key) || IsWithin(status.Key, property)));

    static bool IsWithin(string path, string boundary) =>
        path.Equals(boundary, StringComparison.OrdinalIgnoreCase) ||
        (path.StartsWith(boundary, StringComparison.OrdinalIgnoreCase) && path.Length > boundary.Length && path[boundary.Length] is '.' or '[');
}
