// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Releases;

/// <summary>
/// Knows the releases that cannot share a cluster with the releases before them, however the grain calls compare.
/// </summary>
/// <remarks>
/// Orleans does not negotiate serializers. The concept codec writes its own assembly-qualified name into every value, so the rename of
/// <c language="csharp">Cratis.Orleans</c> to <c language="csharp">Cratis.Choreography</c> in 19.123.1 split the wire in two: a silo of either side throws
/// <see cref="TypeLoadException"/> on a concept value from the other. The grain interface gate cannot see this, so the boundary is
/// recorded here and checked when a silo starts (#5666).
/// </remarks>
public static class ClusterReleaseBoundary
{
    /// <summary>
    /// The first release that names its serializers <c language="csharp">Cratis.Choreography</c>.
    /// </summary>
    public static readonly SemanticVersion SerializerRename = new(19, 123, 1);

    /// <summary>
    /// Finds the releases in a cluster that this release cannot exchange grain calls with because of a boundary.
    /// </summary>
    /// <param name="local">The release of this silo.</param>
    /// <param name="cluster">The releases the cluster publishes.</param>
    /// <returns>The incompatible releases, empty when there are none.</returns>
    public static IReadOnlyList<SemanticVersion> FindIncompatible(SemanticVersion local, ClusterReleaseSummary cluster)
    {
        if (local < SerializerRename)
        {
            return [];
        }

        return [.. cluster.Releases.Select(_ => _.Release).Where(release => release < SerializerRename)];
    }
}
