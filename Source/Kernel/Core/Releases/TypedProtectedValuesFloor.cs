// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sys;

namespace Cratis.Chronicle.Releases;

/// <summary>The reader-first write floor for authenticated JSON kinds in v4 envelopes.</summary>
/// <remarks>
/// A typed envelope extends the signed key identity and requires its rollout conditions as well as a
/// homogeneous, fully published cluster at <see cref="ClusterReleaseBoundary.TypedProtectedValues"/>.
/// Unknown development releases follow the same policy as <see cref="KeyIdentityFloor"/>.
/// </remarks>
internal static class TypedProtectedValuesFloor
{
    /// <summary>Checks whether every published silo can read a typed, signed envelope.</summary>
    /// <param name="releases">The current cluster releases.</param>
    /// <returns>Whether both the signed-identity and typed-value floors are reached.</returns>
    internal static bool IsReached(IClusterReleases? releases)
    {
        if (!KeyIdentityFloor.IsReached(releases))
        {
            return false;
        }

        var release = releases!.Summarize().Releases[0].Release;
        return !KernelVersion.IsKnown(release) || release >= ClusterReleaseBoundary.TypedProtectedValues;
    }
}
