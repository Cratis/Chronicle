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
    /// Gets or sets the single-writer lease for Localhost clustering.
    /// </summary>
    public SingleWriterLease SingleWriterLease { get; set; } = new();

    /// <summary>
    /// Gets the type of cluster membership to use. Defaults to single node localhost clustering;
    /// use <see cref="ClusteringType.MongoDB"/> or <see cref="ClusteringType.Sql"/> to let multiple
    /// nodes sharing the same storage and cluster id form one cluster.
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
    /// Gets or sets whether the Orleans client gateway is enabled. Disable it when only
    /// Chronicle HTTP and gRPC clients connect to this node.
    /// </summary>
    public bool GatewayEnabled { get; set; } = true;

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
    /// address is resolved from the machine's host name. In MongoDB and SQL clustering this
    /// also binds the silo and enabled gateway listeners to that local interface. Localhost
    /// clustering retains Orleans' loopback address selection.
    /// </summary>
    public string? AdvertisedIP { get; set; }

    /// <summary>
    /// Gets how often defunct silo entries are swept out of the cluster membership table when
    /// using <see cref="ClusteringType.MongoDB"/> or <see cref="ClusteringType.Sql"/>. Without the sweep, repeated restarts and failed
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

    /// <summary>
    /// Gets the interval between liveness probes and the timeout for each probe. When unset, keeps the Orleans default.
    /// </summary>
    public TimeSpan? ProbeTimeout { get; set; }

    /// <summary>
    /// Gets the number of missed probes before a silo is suspected to be down. When unset, keeps the Orleans default.
    /// </summary>
    public int? NumMissedProbesLimit { get; set; }

    /// <summary>
    /// Gets the number of non-expired votes required to declare a silo down. When unset, keeps the Orleans default.
    /// </summary>
    public int? NumVotesForDeathDeclaration { get; set; }

    /// <summary>
    /// Gets the interval between fetching membership table updates. When unset, keeps the Orleans default.
    /// </summary>
    public TimeSpan? TableRefreshTimeout { get; set; }

    /// <summary>
    /// Gets the interval between publishing this silo's heartbeat to the membership table. When unset, keeps the Orleans default.
    /// </summary>
    public TimeSpan? IAmAliveTablePublishTimeout { get; set; }

    /// <summary>
    /// Gets the number of other silos each silo probes for liveness. When unset, keeps the Orleans default.
    /// </summary>
    public int? NumProbedSilos { get; set; }

    /// <summary>
    /// Gets how long a grain call waits for its response before timing out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Orleans defaults this to 30 seconds, which is chosen for ordinary request/response traffic
    /// rather than for startup. Some startup work is proportional to how much the server holds -
    /// registering the projections for an event store fans out across every namespace it has - so
    /// the time that work needs grows with the deployment while the 30 seconds does not. A server
    /// that has grown past that point cannot complete startup, and because it cannot, it cannot
    /// start on the next attempt either.
    /// </para>
    /// <para>
    /// Raising this is the operator's way out of that, so it is deliberately configurable rather
    /// than fixed. The default stays at Orleans' own 30 seconds: a call that hangs should still
    /// fail rather than tie a caller up indefinitely.
    /// </para>
    /// </remarks>
    public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
