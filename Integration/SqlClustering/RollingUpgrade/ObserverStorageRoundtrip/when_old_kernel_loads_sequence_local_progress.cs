// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverSequenceCheckpoints;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NSubstitute;
using KernelCheckpoint = Cratis.Chronicle.Storage.Observation.ObserverSequenceCheckpoint;
using KernelConceptTypes = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelFailedPartitions = KernelConcepts::Cratis.Chronicle.Concepts.Observation.FailedPartitions;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.ObserverStorageRoundtrip;

/// <summary>
/// Checks released-kernel readiness with tagged failures and a separate parked checkpoint present.
/// </summary>
public class when_old_kernel_loads_sequence_local_progress : Specification
{
    ReleasedSqlKernelFixture _fixture;
    DbContextOptions<NamespaceDbContext> _options;
    string _connectionString;
    string _identifier;
    string _parkedSequence;
    string _checkpointBefore;
    string _checkpointAfter;
    bool _ready;

    async Task Establish()
    {
        _fixture = new();
        await _fixture.Start();
        var selected = _fixture.Observer;
        _connectionString = selected.ConnectionString;
        _identifier = selected.Identifier;
        _parkedSequence = $"{selected.Owner}-parked";
        await _fixture.StopKernel();
        _options = new DbContextOptionsBuilder<NamespaceDbContext>()
            .UseNpgsql(_connectionString)
            .AddConceptAsSupport()
            .EnableServiceProviderCaching(false)
            .Options;
        await using (var context = new NamespaceDbContext(_options))
        {
            await context.Database.MigrateAsync();
        }
        var database = Substitute.For<IDatabase>();
        database.Namespace(Arg.Any<KernelConceptTypes.EventStoreName>(), Arg.Any<KernelConceptTypes.EventStoreNamespaceName>())
            .Returns(_ => Task.FromResult(new DbContextScope<NamespaceDbContext>(new NamespaceDbContext(_options), static () => { })));
        var failures = new FailedPartitionStorage("old-kernel-store", "old-kernel-namespace", database);
        await failures.Save(_identifier, new KernelFailedPartitions
        {
            Partitions =
            [
                new() { ObserverId = _identifier, Partition = "tagged-partition", EventSequenceId = new(selected.Owner), Attempts = [new() { SequenceNumber = new(1), Messages = ["seeded failure"] }] },
                new() { ObserverId = _identifier, Partition = "untagged-partition", Attempts = [new() { SequenceNumber = new(1), Messages = ["seeded failure"] }] }
            ]
        });
        var checkpoints = new ObserverSequenceCheckpointsStorage("old-kernel-store", "old-kernel-namespace", database);
        await checkpoints.Save(KernelCheckpoint.Fresh(_identifier, _parkedSequence) with
        {
            NextEventSequenceNumber = new(43),
            LastHandledEventSequenceNumber = new(42),
            TailEventSequenceNumber = new(80),
            FailedPartitionCount = new(1),
            Parked = DateTimeOffset.UnixEpoch.AddMinutes(10)
        });
        _checkpointBefore = await CheckpointRow();
    }

    async Task Because()
    {
        // The tag round trip awaits a released-kernel failing-partition seed hook in #5368.
        // Existing readiness checks persisted observer presence, not an old-writer update.
        await _fixture.RestartKernel();
        _ready = true;
        await _fixture.StopKernel();
        _checkpointAfter = await CheckpointRow();
    }

    [Fact] void should_pass_the_released_observer_readiness_wait() => _ready.ShouldBeTrue();
    [Fact] void should_leave_the_checkpoint_row_unchanged() => _checkpointAfter.ShouldEqual(_checkpointBefore);

    async Task Cleanup()
    {
        if (_fixture is not null) await _fixture.DisposeAsync();
    }

    async Task<string> CheckpointRow()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT row_to_json(checkpoint)::text FROM \"ObserverSequenceCheckpoints\" checkpoint WHERE \"ObserverId\" = @observer AND \"EventSequenceId\" = @sequence", connection);
        command.Parameters.AddWithValue("observer", _identifier);
        command.Parameters.AddWithValue("sequence", _parkedSequence);
        return (string)(await command.ExecuteScalarAsync() ?? throw new InvalidOperationException("The parked checkpoint row is missing."));
    }
}
