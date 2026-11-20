// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_CountingDbConnection;

public class when_running_commands_on_the_connection : Specification
{
    CommandCounter _counter = default!;
    CommandSnapshot _snapshot = default!;

    async Task Because()
    {
        _counter = new();
        await using var connection = new CountingDbConnection(new SqliteConnection("Data Source=:memory:"), _counter);
        await connection.OpenAsync();
        await using var create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE t (id INTEGER)";
        await create.ExecuteNonQueryAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = "INSERT INTO t VALUES (1)";
        await insert.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        await using var abandoned = await connection.BeginTransactionAsync();
        await abandoned.RollbackAsync();
        _snapshot = _counter.Snapshot();
    }

    [Fact] void should_count_direct_commands() => _snapshot.ByType["create"].ShouldEqual(1);
    [Fact] void should_count_statements_in_a_transaction() => _snapshot.ByType["insert"].ShouldEqual(1);
    [Fact] void should_count_commit() => _snapshot.ByType[SqlVerb.Commit].ShouldEqual(1);
    [Fact] void should_count_rollback() => _snapshot.ByType[SqlVerb.Rollback].ShouldEqual(1);
    [Fact] void should_not_count_begin() => _snapshot.Total.ShouldEqual(4);
}
