// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations;
using Microsoft.EntityFrameworkCore;
using EventSequenceMutationCoverage = Cratis.Chronicle.Storage.EventSequences.Mutations.EventSequenceMutationCoverage;
using EventSequenceMutationOrdinal = Cratis.Chronicle.Concepts.EventSequences.Mutations.EventSequenceMutationOrdinal;

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
    /// Gets or sets retained alert incidents.
    /// </summary>
    public DbSet<Alerts.AlertIncidentEntity> AlertIncidents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the changesets DbSet.
    /// </summary>
    public DbSet<Changesets.Changeset> Changesets { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identities DbSet.
    /// </summary>
    public DbSet<Identities.Identity> Identities { get; set; } = null!;

    /// <summary>
    /// Gets or sets the failed partitions DbSet.
    /// </summary>
    public DbSet<FailedPartitions.FailedPartition> FailedPartitions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the recommendations DbSet.
    /// </summary>
    public DbSet<Recommendations.Recommendation> Recommendations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the behavior patterns DbSet.
    /// </summary>
    public DbSet<Patterns.BehaviorPattern> BehaviorPatterns { get; set; } = null!;

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

    /// <summary>
    /// Gets or sets the event sequence mutation heads DbSet.
    /// </summary>
    public DbSet<EventSequenceMutationHeadEntry> EventSequenceMutationHeads { get; set; } = null!;

    /// <summary>
    /// Gets or sets the event sequence mutation history DbSet.
    /// </summary>
    public DbSet<EventSequenceMutationHistoryEntry> EventSequenceMutationHistory { get; set; } = null!;

    /// <summary>
    /// Gets or sets the event seeding data DbSet.
    /// </summary>
    public DbSet<Seeding.EventSeedsEntity> EventSeeds { get; set; } = null!;

    /// <summary>
    /// Gets or sets the projection futures DbSet.
    /// </summary>
    public DbSet<Projections.ProjectionFutureEntity> ProjectionFutures { get; set; } = null!;

    /// <summary>
    /// Gets or sets the encryption keys DbSet.
    /// </summary>
    public DbSet<Encryption.EncryptionKey> EncryptionKeys { get; set; } = null!;

    /// <summary>
    /// Gets or sets the recorded encryption key erasures DbSet.
    /// </summary>
    public DbSet<Encryption.EncryptionKeyErasure> EncryptionKeyErasures { get; set; } = null!;

    /// <summary>Gets or sets isolated request archives and silo protocol records.</summary>
    public DbSet<Encryption.EncryptionKeyFenceRecord> EncryptionKeyFenceRecords { get; set; } = null!;

    /// <summary>Gets or sets the authorities for external key-material stores.</summary>
    public DbSet<Encryption.EncryptionKeyExternalFence> EncryptionKeyExternalFences { get; set; } = null!;

    /// <summary>
    /// Gets or sets the kernel-internal secrets DbSet.
    /// </summary>
    public DbSet<Encryption.InternalSecret> InternalSecrets { get; set; } = null!;

    /// <summary>
    /// Gets or sets the closed streams DbSet.
    /// </summary>
    public DbSet<ClosedStreamEntry> ClosedStreams { get; set; } = null!;

    /// <summary>
    /// Gets or sets the in-flight events DbSet.
    /// </summary>
    public DbSet<InFlightEvents.InFlightEventEntry> InFlightEvents { get; set; } = null!;

    /// <summary>
    /// Gets or sets the observer handled counts DbSet.
    /// </summary>
    public DbSet<ObserverHandledCounts.ObserverHandledCountEntry> ObserverHandledCounts { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        EventStores.Occurrences.OccurrenceModel.Configure(modelBuilder);
        if (Database.IsSqlServer())
        {
            Namespaces.EventSequences.ExactIdentifiers.RegisterByteLength(modelBuilder);
        }

        EventStores.Occurrences.OccurrenceModel.ConfigureReceipts(modelBuilder);
        modelBuilder
            .Entity<Observers.ObserverDefinitionChangeEntry>(entity =>
            {
                entity.ToTable(WellKnownTableNames.ObserverDefinitionChangeRows);
                entity.HasKey(row => row.Id);
                entity.Property(row => row.Id).HasMaxLength(64);
                entity.Property(row => row.Observer).HasMaxLength(64);
                entity.Property(row => row.Family).HasMaxLength(64);
                entity.Property(row => row.Identity).HasMaxLength(64);
                entity.Property(row => row.Position).HasPrecision(20, 0);
                entity.HasIndex(row => new { row.Observer, row.Family, row.Revision, row.Id });
                entity.HasIndex(row => new { row.Observer, row.Family, row.Id });
            })
            .Entity<EventSequences.EventSequenceState>(entity =>
            {
                entity.Property(row => row.ConstraintIndexWatermark).HasPrecision(20, 0);
                entity.Property(row => row.ConstraintIndexCatchUpBelow).HasPrecision(20, 0);
                entity.Property(row => row.MutationEpoch).HasPrecision(20, 0);
            })
            .Entity<ClosedStreamEntry>(entity =>
            {
                entity.ToTable(WellKnownTableNames.ClosedStreams);
                entity.HasKey(e => new { e.EventSequenceId, e.StreamType, e.StreamId, e.EventSourceId, e.EventSourceType, e.Owner, e.Dimensions });
                entity.Property(e => e.SequenceNumber).HasPrecision(20, 0);
            })
            .Entity<InFlightEvents.InFlightEventEntry>(entity =>
            {
                entity.ToTable(WellKnownTableNames.InFlightEvents);
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventSequenceNumber).HasPrecision(20, 0);
                entity.HasIndex(e => new { e.ObserverId, e.Partition, e.EventSequenceNumber });
            })
            .Entity<ObserverHandledCounts.ObserverHandledCountEntry>(entity =>
            {
                entity.ToTable(WellKnownTableNames.ObserverHandledCounts);
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ObserverId, e.Partition, e.EventTypeId }).IsUnique();
            })
            .Entity<Patterns.BehaviorPattern>(entity =>
            {
                entity.ToTable(WellKnownTableNames.BehaviorPatterns);
                entity.HasKey(e => new { e.GroupingKey, e.FacetSetHash });
            })
            .Entity<EventSequenceMutationHeadEntry>(entity =>
            {
                entity.ToTable(WellKnownTableNames.EventSequenceMutationHeads);
                entity.HasKey(e => e.EventSequenceId);
                entity.Property(e => e.EventSequenceId).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Coverage).HasDefaultValue(EventSequenceMutationCoverage.Untracked).IsRequired();
                entity.Property(e => e.LastAssignedOrdinal).HasDefaultValue(EventSequenceMutationOrdinal.NotSet).IsRequired();
                entity.Property(e => e.ActiveMutationId).IsRequired(false);
                entity.Property(e => e.ActiveOrdinal).IsRequired(false);
                entity.Property(e => e.ActiveStateVersion).IsRequired(false);
                entity.Property(e => e.ActiveOriginSequence).HasMaxLength(200).IsRequired(false);
                entity.Property(e => e.ActiveOriginSequenceNumber).IsRequired(false);
                entity.Property(e => e.ActiveKind).IsRequired(false);
                entity.Property(e => e.ActiveCommandPayload).IsRequired(false);
                entity.Property(e => e.ActiveCommandHash).HasMaxLength(64).IsRequired(false);
                entity.Property(e => e.ActiveTargetStart).IsRequired(false);
                entity.Property(e => e.ActiveTargetEndExclusive).IsRequired(false);
                entity.Property(e => e.ActiveTargetExpectedCount).IsRequired(false);
                entity.Property(e => e.ActiveDefinitionDigestV1)
                    .HasConversion(d => EventSequenceMutationDigestColumns.DefinitionDigestToHex(d!), s => EventSequenceMutationDigestColumns.DefinitionDigestFromHex(s))
                    .HasMaxLength(64)
                    .IsRequired(false);
                entity.Property(e => e.ActiveDefinitionDigestVersion).IsRequired(false);
                entity.Property(e => e.ActivePhase).IsRequired(false);
                entity.Property(e => e.ActiveBlockedFrom).IsRequired(false);
                entity.Property(e => e.ActiveRepairState).IsRequired(false);
            })
            .Entity<EventSequenceMutationHistoryEntry>(entity =>
            {
                entity.ToTable(WellKnownTableNames.EventSequenceMutationHistory);
                entity.HasKey(e => new { e.EventSequenceId, e.Ordinal });
                entity.HasIndex(e => e.MutationId).IsUnique();
                entity.Property(e => e.EventSequenceId).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Ordinal).IsRequired();
                entity.Property(e => e.MutationId).IsRequired();
                entity.Property(e => e.Kind).IsRequired();
                entity.Property(e => e.TargetStart).IsRequired();
                entity.Property(e => e.TargetEndExclusive).IsRequired();
                entity.Property(e => e.TargetExpectedCount).IsRequired();
                entity.Property(e => e.TerminalPhase).IsRequired();
                entity.Property(e => e.RepairState).IsRequired();
                entity.Property(e => e.DigestVersion).IsRequired();
                entity.Property(e => e.ArchivedAt).IsRequired();
                entity.Property(e => e.FinalStateVersion).IsRequired();
                entity.Property(e => e.DefinitionDigestV1)
                    .HasConversion(d => EventSequenceMutationDigestColumns.DefinitionDigestToHex(d), s => EventSequenceMutationDigestColumns.DefinitionDigestFromHex(s))
                    .HasMaxLength(64)
                    .IsRequired();
                entity.Property(e => e.ReceiptDigestV1)
                    .HasConversion(d => EventSequenceMutationDigestColumns.ReceiptDigestToHex(d), s => EventSequenceMutationDigestColumns.ReceiptDigestFromHex(s))
                    .HasMaxLength(64)
                    .IsRequired();
            })
            .Entity<ReplayedModels.ReplayedModelOccurrence>(entity =>
            {
                entity.ToTable(WellKnownTableNames.ReplayedReadModels);
                entity.HasKey(e => new { e.ObserverId, e.Started });
            });

        string incidentCollation;
        if (Database.IsSqlServer())
        {
            incidentCollation = "Latin1_General_100_BIN2";
        }
        else if (Database.IsNpgsql())
        {
            incidentCollation = "C";
        }
        else
        {
            incidentCollation = "BINARY";
        }

        if (Database.IsSqlServer())
        {
            modelBuilder.Entity<Encryption.EncryptionKey>().Property(e => e.Identifier).UseCollation(Encryption.EncryptionKeyIdentifierColumn.Collation);
            modelBuilder.Entity<Encryption.EncryptionKeyErasure>().Property(e => e.Identifier).UseCollation(Encryption.EncryptionKeyIdentifierColumn.Collation);
            modelBuilder.Entity<Encryption.EncryptionKeyExternalFence>().Property(e => e.Identifier).UseCollation(Encryption.EncryptionKeyIdentifierColumn.Collation);
            UseExactClosedStreamColumns(modelBuilder);
        }

        modelBuilder.Entity<Alerts.AlertIncidentEntity>(entity =>
        {
            entity.ToTable(WellKnownTableNames.AlertIncidents);
            entity.HasKey(row => row.Id);
            entity.Property(row => row.Id).HasMaxLength(32);
            entity.Property(row => row.EventStore).HasMaxLength(200);
            entity.Property(row => row.Namespace).HasMaxLength(200);
            foreach (var property in entity.Metadata.GetProperties().Where(property => property.ClrType == typeof(string)))
            {
                entity.Property(property.Name).UseCollation(incidentCollation);
            }
            entity.HasIndex(row => new { row.EventStore, row.IsOpen, row.RaisedSequenceNumber, row.Id });
            entity.HasIndex(row => new { row.EventStore, row.Namespace, row.IsOpen, row.RaisedSequenceNumber, row.Id });
            entity.HasIndex(row => new { row.IsOpen, row.EventStore, row.Namespace });
        });

        // SQL Server text equality pads trailing spaces even under binary collations. Registry keys
        // must preserve the canonical identity exactly, so use UTF-8 bytes rather than text keys there.
        if (Database.IsSqlServer())
        {
            modelBuilder.Entity<EventSequenceMutationHeadEntry>().Property(e => e.EventSequenceId)
                .HasConversion(id => System.Text.Encoding.UTF8.GetBytes(id.Value), bytes => new Concepts.EventSequences.EventSequenceId(System.Text.Encoding.UTF8.GetString(bytes)))
                .HasColumnType("varbinary(600)").HasMaxLength(600);
            modelBuilder.Entity<EventSequenceMutationHistoryEntry>().Property(e => e.EventSequenceId)
                .HasConversion(id => System.Text.Encoding.UTF8.GetBytes(id.Value), bytes => new Concepts.EventSequences.EventSequenceId(System.Text.Encoding.UTF8.GetString(bytes)))
                .HasColumnType("varbinary(600)").HasMaxLength(600);
        }

        // Match the column mappings to the provider-native JSON type the migrations create
        // (jsonb on Npgsql), so EF Core sends parameters with the correct OID. PostgreSQL is
        // the only provider that requires this because its jsonb type rejects implicit
        // casts from text. See arc-issues.md for the upstream tracking issue in Cratis.Arc.
        if (!Database.IsNpgsql())
        {
            return;
        }

        foreach (var (entityType, propertyName) in NamespaceJsonStringColumns.All)
        {
            modelBuilder.Entity(entityType).Property(propertyName).HasColumnType("jsonb");
        }
    }

    static void UseExactClosedStreamColumns(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ClosedStreamEntry>();
        foreach (var column in new[] { nameof(ClosedStreamEntry.StreamType), nameof(ClosedStreamEntry.StreamId), nameof(ClosedStreamEntry.EventSourceId), nameof(ClosedStreamEntry.EventSourceType) })
        {
            entity.Property(column).HasMaxLength(ClosedStreamColumn.MaxLength).UseCollation(ClosedStreamColumn.Collation);
        }
    }
}
