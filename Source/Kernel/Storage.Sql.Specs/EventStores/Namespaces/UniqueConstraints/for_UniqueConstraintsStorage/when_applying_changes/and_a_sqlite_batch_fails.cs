// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_applying_changes;

public class and_a_sqlite_batch_fails : given.batch_recovery_scenarios
{
    SqliteConnection _connection;

    async Task Establish()
    {
        _connection = new($"DataSource=recovery-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await _connection.OpenAsync();
        Configure(builder => builder.UseSqlite(_connection.ConnectionString), false, () => _connection.DisposeAsync().AsTask());
    }

    Task Because() => CheckRecovery();

    [Fact] void should_rollback_late_failures_without_replaying_or_counting_collisions() => VerifyRollback();
    [Fact] void should_replay_only_conflicts_and_preserve_a_committed_winner() => VerifyRecovery();
}
