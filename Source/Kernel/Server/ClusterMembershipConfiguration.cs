// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Orleans.Configuration;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Applies Chronicle's membership configuration to Orleans.
/// </summary>
internal static class ClusterMembershipConfiguration
{
    /// <summary>
    /// Applies explicit membership settings while preserving unset Orleans options.
    /// </summary>
    /// <param name="clustering">The Chronicle clustering configuration.</param>
    /// <param name="options">The Orleans membership options.</param>
    internal static void Apply(Clustering clustering, ClusterMembershipOptions options)
    {
        options.DefunctSiloCleanupPeriod = clustering.DefunctSiloCleanupPeriod > TimeSpan.Zero
            ? clustering.DefunctSiloCleanupPeriod
            : null;
        options.DefunctSiloExpiration = clustering.DefunctSiloExpiration;
        options.ProbeTimeout = clustering.ProbeTimeout ?? options.ProbeTimeout;
        options.NumMissedProbesLimit = clustering.NumMissedProbesLimit ?? options.NumMissedProbesLimit;
        options.NumVotesForDeathDeclaration = clustering.NumVotesForDeathDeclaration ?? options.NumVotesForDeathDeclaration;
        options.TableRefreshTimeout = clustering.TableRefreshTimeout ?? options.TableRefreshTimeout;
        options.IAmAliveTablePublishTimeout = clustering.IAmAliveTablePublishTimeout ?? options.IAmAliveTablePublishTimeout;
        options.NumProbedSilos = clustering.NumProbedSilos ?? options.NumProbedSilos;
    }
}
