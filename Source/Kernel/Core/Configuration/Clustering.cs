// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for clustering.
/// </summary>
public class Clustering
{
    /// <summary>
    /// Gets the cluster roles configuration.
    /// </summary>
    public ClusterRoles Roles { get; set; } = new();

    /// <summary>
    /// Gets the type of cluster membership to use. Defaults to single node localhost clustering;
    /// use <see cref="ClusteringType.MongoDB"/> to let multiple nodes sharing the same MongoDB
    /// storage and cluster id form one cluster.
    /// </summary>
    public ClusteringType Type { get; set; } = ClusteringType.Localhost;

    /// <summary>
    /// Gets the port silo to silo communication happens on. Must differ per node when multiple
    /// nodes run on the same machine.
    /// </summary>
    public int SiloPort { get; set; } = 11111;

    /// <summary>
    /// Gets the port the silo's client gateway listens on. Must differ per node when multiple
    /// nodes run on the same machine.
    /// </summary>
    public int GatewayPort { get; set; } = 30000;

    /// <summary>
    /// Gets the cluster id - all nodes that should form one cluster must share it.
    /// </summary>
    public string ClusterId { get; set; } = "chronicle";

    /// <summary>
    /// Gets the service id - all nodes that should form one cluster must share it.
    /// </summary>
    public string ServiceId { get; set; } = "chronicle";

    /// <summary>
    /// Gets the IP address the silo advertises to other cluster members. When not set, the
    /// address is resolved from the machine's host name. Set it explicitly (e.g. 127.0.0.1) when
    /// running multiple nodes on one machine.
    /// </summary>
    public string? AdvertisedIP { get; set; }

    /// <summary>
    /// Gets how often defunct silo entries are swept out of the cluster membership table when
    /// using <see cref="ClusteringType.MongoDB"/>. Without the sweep, repeated restarts and failed
    /// rollouts accumulate dead silo entries forever, and new nodes stall validating against them
    /// when joining. Set to <see cref="TimeSpan.Zero"/> to disable the sweep.
    /// </summary>
    public TimeSpan DefunctSiloCleanupPeriod { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the age at which a defunct membership entry is removed by the sweep. A node never
    /// reuses a silo identity (address + generation), so a dead entry only has diagnostic value —
    /// a few hours keeps recent history around while keeping the table small enough for new nodes
    /// to join quickly.
    /// </summary>
    public TimeSpan DefunctSiloExpiration { get; set; } = TimeSpan.FromHours(3);
}
