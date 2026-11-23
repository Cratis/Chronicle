// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_applying_changes;

[Collection(SqlServerCollection.Name)]
public class and_a_sql_server_batch_fails(SqlServerFixture fixture) : given.batch_recovery_scenarios
{
    async Task Establish()
    {
        var connection = await fixture.CreateDatabase();
        Configure(builder => builder.UseSqlServer(connection), true, () => fixture.QueueDatabaseDrop(connection));
    }

    Task Because() => CheckRecovery();

    [Fact] void should_rollback_late_failures_without_replaying_or_counting_collisions() => VerifyRollback();
    [Fact] void should_replay_only_conflicts_and_preserve_a_committed_winner() => VerifyRecovery();
}
