// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using KernelFailedPartition = Cratis.Chronicle.Concepts.Observation.FailedPartition;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionStorage.when_saving;

public class and_a_later_upsert_fails : given.a_failed_partition_storage
{
    readonly Guid _resolvedId = Guid.NewGuid();
    readonly Guid _existingId = Guid.NewGuid();
    readonly Guid _failingId = Guid.NewGuid();
    Concepts.Observation.FailedPartitions _state;
    Exception _error;
    List<FailedPartition> _saved;

    async Task Establish()
    {
        var resolved = new KernelFailedPartition { Id = _resolvedId, ObserverId = "observer", Partition = new Key("resolved", ArrayIndexers.NoIndexers) };
        var existing = new KernelFailedPartition { Id = _existingId, ObserverId = "observer", Partition = new Key("original", ArrayIndexers.NoIndexers) };
        await using (var context = CreateContext())
        {
            context.FailedPartitions.AddRange(resolved.ToEntity(), existing.ToEntity());
            await context.SaveChangesAsync();
        }
        _state = new() { Partitions = [resolved] };
        _state.Remove(resolved.Partition);
        existing.Partition = new Key("updated", ArrayIndexers.NoIndexers);
        _state.Partitions = [existing, new KernelFailedPartition { Id = _failingId, ObserverId = "observer" }];
        var failure = new fail_upsert(_failingId);
        _database.Namespace(_eventStore, _namespace)
            .Returns(_ => new DbContextScope<NamespaceDbContext>(CreateContext(failure), () => { }));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _storage.Save("observer", _state));
        await using var context = CreateContext();
        _saved = await context.FailedPartitions.ToListAsync();
    }

    [Fact] void should_surface_the_later_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_restore_the_resolved_partition() => _saved.Exists(row => row.Id == _resolvedId).ShouldBeTrue();
    [Fact] void should_roll_back_the_earlier_upsert() => _saved.Single(row => row.Id == _existingId).Partition.ShouldEqual("original");
    [Fact] void should_not_insert_the_failed_row() => _saved.Exists(row => row.Id == _failingId).ShouldBeFalse();
    [Fact] void should_keep_only_the_original_rows() => _saved.Count.ShouldEqual(2);

    sealed class fail_upsert(Guid id) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Fail(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Fail(command);
            return ValueTask.FromResult(result);
        }

        void Fail(DbCommand command)
        {
            if (command.CommandText.Contains("INSERT", StringComparison.OrdinalIgnoreCase) &&
                command.Parameters.Cast<DbParameter>().Any(parameter => string.Equals(parameter.Value?.ToString(), id.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Injected later upsert failure");
            }
        }
    }
}
