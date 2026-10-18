// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_ensuring_a_sqlite_table;

public class and_the_table_is_already_ensured : given.a_sqlite_database
{
    string _target;
    bool _completedWhileLocked;
    bool _otherTableWaited;

    async Task Establish()
    {
        await using var scope = await _database.EventSequenceTable("store", "namespace", "events");
        _target = scope.DbContext.Database.GetConnectionString()!;
    }

    async Task Because()
    {
        var semaphore = MigrationSemaphore(_target);
        await semaphore.WaitAsync();
        try
        {
            // Holding the semaphore is the barrier: no timer or scheduling delay decides success.
            var repeated = Track(EnsureTable("events"));
            _completedWhileLocked = repeated.IsCompletedSuccessfully;
            var other = Track(EnsureTable("other"));
            _otherTableWaited = !other.IsCompleted;
        }
        finally
        {
            semaphore.Release();
            await CompleteOperations();
        }
    }

    [Fact] void should_not_wait_on_the_migration_lock() => _completedWhileLocked.ShouldBeTrue();
    [Fact] void should_still_lock_for_another_table() => _otherTableWaited.ShouldBeTrue();

    async Task EnsureTable(string table)
    {
        await using var scope = await _database.EventSequenceTable("store", "namespace", table);
    }
}
