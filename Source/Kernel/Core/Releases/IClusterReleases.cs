// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Releases;

/// <summary>
/// Provides the kernel releases published by the cluster's silos.
/// </summary>
public interface IClusterReleases
{
    /// <summary>
    /// Selects candidates running at least this silo's release. Unstamped development builds keep all candidates.
    /// </summary>
    /// <param name="candidates">The eligible placement candidates.</param>
    /// <returns>The candidates whose published release is new enough.</returns>
    IReadOnlyList<SiloAddress> AtLeastAsNewAsThisSilo(IEnumerable<SiloAddress> candidates);

    /// <summary>
    /// Summarizes the cluster's published releases without exposing untrusted manifest text.
    /// </summary>
    /// <returns>A bounded release summary.</returns>
    ClusterReleaseSummary Summarize();
}
