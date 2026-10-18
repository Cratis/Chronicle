// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
namespace Cratis.Chronicle.Storage.Sql.for_Database.when_wiping;

public class and_a_sqlite_migration_starts_during_the_wipe : given.a_sqlite_database
{
    readonly TaskCompletionSource _clearing = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _migrationWaited;
    string[] _pending;

    async Task Establish()
    {
        await using var context = Context();
        await Migrate(context);
        _eventSequenceMigrator.When(migrator => migrator.ClearMigrationCache(string.Empty)).Do(_ =>
        {
            _clearing.TrySetResult();
            _release.Task.WaitAsync(Deadline).GetAwaiter().GetResult();
        });
    }

    async Task Because()
    {
        _ = Track(Task.Run(_database.Wipe));
        try
        {
            await _clearing.Task.WaitAsync(Deadline);
            var migration = StartMigration();
            _migrationWaited = !migration.IsCompleted;
        }
        finally
        {
            _release.TrySetResult();
            await CompleteOperations();
        }
        _pending = await PendingMigrations();
    }

    [Fact] void should_hold_new_migrations_until_the_wipe_finishes() => _migrationWaited.ShouldBeTrue();
    [Fact] void should_recreate_the_schema_after_the_wipe() => _pending.ShouldBeEmpty();

    void Destroy() => _release.TrySetResult();
}
#endif
