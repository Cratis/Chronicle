// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Selects the Localhost storage guard independently of the storage provider.
/// </summary>
internal static class LocalhostClusteringGuard
{
    /// <summary>
    /// Determines whether to lease, warn, or leave the host unchanged.
    /// </summary>
    /// <param name="options">The Chronicle configuration.</param>
    /// <returns>The selected protection.</returns>
    internal static LocalhostClusteringGuardAction Decide(ChronicleOptions options)
    {
        if (options.Clustering.Type != ClusteringType.Localhost || string.Equals(options.Storage.Type, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            return LocalhostClusteringGuardAction.None;
        }

        if (options.Clustering.SingleWriterLease.AppliesTo(options.Clustering.Type, options.Storage.Type))
        {
            return LocalhostClusteringGuardAction.Lease;
        }

        return ConnectionStringLocality.IsNonLocal(options.Storage.ConnectionDetails) ? LocalhostClusteringGuardAction.Warn : LocalhostClusteringGuardAction.None;
    }
}
