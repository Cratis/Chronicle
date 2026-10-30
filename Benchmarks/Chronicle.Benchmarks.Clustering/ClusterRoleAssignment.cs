// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Describes the roles assigned to one benchmark silo.
/// </summary>
/// <param name="EventSequences">Whether the silo hosts event sequences.</param>
/// <param name="Observers">Whether the silo hosts observers.</param>
/// <param name="Jobs">Whether the silo hosts jobs when that role is supported.</param>
internal sealed record ClusterRoleAssignment(bool EventSequences, bool Observers, bool Jobs)
{
    /// <summary>
    /// Gets the role assignment for a silo in the selected topology.
    /// </summary>
    /// <param name="topology">The selected cluster topology.</param>
    /// <param name="isFirstSilo">Whether this is silo zero.</param>
    /// <returns>The roles to configure on the silo.</returns>
    internal static ClusterRoleAssignment For(ClusterTopology topology, bool isFirstSilo) => topology switch
    {
        ClusterTopology.TwoSilosWithSplitRoles => new(isFirstSilo, !isFirstSilo, true),
        ClusterTopology.TwoSilosWithSplitJobs => new(isFirstSilo, isFirstSilo, !isFirstSilo),
        _ => new(true, true, true)
    };

    /// <summary>
    /// Finds the optional Jobs role and validates split-jobs support before startup.
    /// </summary>
    /// <param name="roles">The kernel's cluster role options.</param>
    /// <param name="topology">The selected cluster topology.</param>
    /// <returns>The writable boolean Jobs property, or null on kernels without that role.</returns>
    /// <exception cref="InvalidOperationException">The split-jobs topology requires an unavailable Jobs role.</exception>
    internal static PropertyInfo? JobsProperty(object roles, ClusterTopology topology)
    {
        var property = roles.GetType().GetProperty("Jobs");
        if (property?.PropertyType != typeof(bool) || property.SetMethod?.IsPublic != true)
        {
            if (topology == ClusterTopology.TwoSilosWithSplitJobs)
            {
                throw new InvalidOperationException("split-jobs needs the cluster Jobs role (Chronicle#5167)");
            }

            return null;
        }

        return property;
    }
}
