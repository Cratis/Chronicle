// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Represents the DbContext for an event store namespace.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class NamespaceDbContext(DbContextOptions<NamespaceDbContext> options) : BaseDbContext(options)
{
    /// <summary>
    /// Gets or sets the observer state DbSet.
    /// </summary>
    public DbSet<Observers.ObserverState> Observers { get; set; } = null!;

    /// <summary>
    /// Gets or sets the changesets DbSet.
    /// </summary>
    public DbSet<Changesets.Changeset> Changesets { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identities DbSet.
    /// </summary>
    public DbSet<Identities.Identity> Identities { get; set; } = null!;

    /// <summary>
    /// Gets or sets the jobs DbSet.
    /// </summary>
    public DbSet<Jobs.Job> Jobs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the job steps DbSet.
    /// </summary>
    public DbSet<JobSteps.JobStep> JobSteps { get; set; } = null!;

    /// <summary>
    /// Gets or sets the failed partitions DbSet.
    /// </summary>
    public DbSet<FailedPartitions.FailedPartition> FailedPartitions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the recommendations DbSet.
    /// </summary>
    public DbSet<Recommendations.Recommendation> Recommendations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the replay contexts DbSet.
    /// </summary>
    public DbSet<ReplayContexts.ReplayContextEntry> ReplayContexts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the replayed model occurrences DbSet.
    /// </summary>
    public DbSet<ReplayedModels.ReplayedModelOccurrence> ReplayedModels { get; set; } = null!;

    /// <summary>
    /// Gets or sets the event sequence states DbSet.
    /// </summary>
    public DbSet<EventSequences.EventSequenceState> EventSequences { get; set; } = null!;
}
