// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Represents the cluster shapes the benchmarks measure the same workload against.
/// </summary>
public enum ClusterTopology
{
    /// <summary>
    /// One silo hosting every grain type. The baseline every existing Chronicle benchmark measures.
    /// </summary>
    SingleSilo = 0,

    /// <summary>
    /// Two silos, both allowed to host every grain type. The realistic scale out shape, where
    /// placement is free to keep a grain local to the caller.
    /// </summary>
    TwoSilos = 1,

    /// <summary>
    /// Two silos with the roles split so that event sequences live on one silo and observers on the
    /// other. Every event therefore crosses the silo boundary, which isolates the cost of the hop.
    /// </summary>
    TwoSilosWithSplitRoles = 2
}
