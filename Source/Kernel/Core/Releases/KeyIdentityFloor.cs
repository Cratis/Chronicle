// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sys;

namespace Cratis.Chronicle.Releases;

/// <summary>
/// The cluster compatibility floor for writing protected values that carry a verifiable managed-key identity (#4619).
/// </summary>
/// <remarks>
/// A silo that predates the identity envelope cannot read it, so it is written only while every silo is published, every silo
/// runs the same release and that release is at or after <see cref="ClusterReleaseBoundary.KeyIdentity"/>. A release that cannot be
/// identified (a development build) reaches the floor. In a mixed cluster values are written in the earlier format.
/// </remarks>
public static class KeyIdentityFloor
{
    /// <summary>
    /// Gets whether the floor is reached.
    /// </summary>
    /// <param name="releases">The <see cref="IClusterReleases"/> that knows the release of every silo, or null when it is not known.</param>
    /// <returns>True when values may be written with a key identity; otherwise false.</returns>
    public static bool IsReached(IClusterReleases? releases)
    {
        if (releases is null)
        {
            return false;
        }

        var summary = releases.Summarize();
        if (summary is null || summary.Unpublished > 0 || summary.Omitted > 0 || summary.Releases.Count != 1)
        {
            return false;
        }

        var release = summary.Releases[0].Release;
        return !KernelVersion.IsKnown(release) || release >= ClusterReleaseBoundary.KeyIdentity;
    }
}
