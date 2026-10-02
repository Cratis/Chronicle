// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_replay_promotions;

public class and_two_contexts_create_it_on_sqlite : given.two_contexts_creating_the_marker_table<SqlSinkHarness>
{
    Task Because() => CreateConcurrently();

    [Fact] void should_complete_both_migrations() => _error.ShouldBeNull();
    [Fact] void should_attempt_two_real_creates() => _createAttempts.ShouldEqual(2);
    [Fact] void should_recover_from_the_provider_duplicate_table_error() => ((SqliteException)_duplicateError!).SqliteErrorCode.ShouldEqual(1);
    [Fact] void should_leave_an_empty_queryable_marker_table() => _markerCount.ShouldEqual(0);
}
