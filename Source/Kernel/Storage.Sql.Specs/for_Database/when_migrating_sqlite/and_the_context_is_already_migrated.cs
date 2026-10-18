// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_migrating_sqlite;

public class and_the_context_is_already_migrated : given.a_sqlite_database
{
    bool _completedWhileLocked;

    async Task Establish() => await StartMigration();

    async Task Because()
    {
        var semaphore = MigrationSemaphore(_connectionString);
        await semaphore.WaitAsync();
        try
        {
            var repeated = StartMigration();
            _completedWhileLocked = repeated.IsCompletedSuccessfully;
        }
        finally
        {
            semaphore.Release();
            await CompleteOperations();
        }
    }

    [Fact] void should_not_wait_on_the_migration_lock() => _completedWhileLocked.ShouldBeTrue();
}
