// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_migrating_sqlite;

public class and_two_instances_use_the_same_connection_string : given.a_sqlite_database
{
    readonly given.a_paused_migration _pause = new();
    bool _secondWaited;
    string[] _pending;

    async Task Because()
    {
        _ = StartMigration(interceptors: _pause);
        try
        {
            await _pause.Entered;
            var second = StartMigration(database: CreateDatabase());
            _secondWaited = !second.IsCompleted;
        }
        finally
        {
            _pause.Release();
            await CompleteOperations();
        }

        _pending = await PendingMigrations();
    }

    [Fact] void should_serialize_both_migrations() => _secondWaited.ShouldBeTrue();
    [Fact] void should_complete_both_migrations() => _pending.ShouldBeEmpty();

    void Destroy() => _pause.Release();
}
