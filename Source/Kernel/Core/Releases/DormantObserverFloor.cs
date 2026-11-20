// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sys;

namespace Cratis.Chronicle.Releases;

/// <summary>
/// The cluster compatibility floor for releasing dormant observers and waking them (decision 0034).
/// </summary>
/// <remarks>
/// The floor is the one decisions 0017 and 0018 share: it is read from the release every silo publishes in its manifest.
/// A dormant observer has no activation and no timer, so the silos that hold its queue entry and the silo that wakes it must
/// all understand dormancy. The floor is therefore reached only while every silo is published, every silo runs the same release
/// and that release is at or after <see cref="ClusterReleaseBoundary.DormantObservers"/>. A release that cannot be
/// identified (a development build) reaches the floor, as it does for observer placement. In a mixed cluster the floor is not
/// reached, and observers are never released or woken from a record, which is the behavior before dormancy existed.
/// </remarks>
public static class DormantObserverFloor
{
    /// <summary>
    /// Gets whether the floor is reached.
    /// </summary>
    /// <param name="releases">The <see cref="IClusterReleases"/> that knows the release of every silo, or null when it is not known.</param>
    /// <returns>True when dormant observers may be released and woken; otherwise false.</returns>
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
        return !KernelVersion.IsKnown(release) || release >= ClusterReleaseBoundary.DormantObservers;
    }
}
