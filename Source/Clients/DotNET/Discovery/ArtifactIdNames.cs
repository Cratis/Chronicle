// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Extracts comparable names from artifact identities.
/// </summary>
internal static class ArtifactIdNames
{
    /// <summary>
    /// Extracts a short name without namespace, nesting, or generic arguments.
    /// </summary>
    /// <param name="id">The artifact identity.</param>
    /// <returns>The name segment.</returns>
    internal static string NameSegmentOf(string id)
    {
        var arguments = id.IndexOf('[', StringComparison.Ordinal);
        var name = arguments < 0 ? id : id[..arguments];
        name = name[(Math.Max(name.LastIndexOf('.'), name.LastIndexOf('+')) + 1)..];
        var arity = name.IndexOf('`', StringComparison.Ordinal);

        return arity < 0 ? name : name[..arity];
    }

    /// <summary>
    /// Gets names that could identify the artifact before a rename or move.
    /// </summary>
    /// <param name="artifact">The current artifact.</param>
    /// <returns>The candidate names.</returns>
    internal static IEnumerable<string> CandidateNamesOf(DiscoveredArtifact artifact) =>
        artifact.ClrType is null || artifact.ClrType == typeof(object)
            ? [NameSegmentOf(artifact.Id)]
            : [NameSegmentOf(artifact.Id), NameSegmentOf(artifact.ClrType.Name)];
}
