// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
namespace Cratis.Chronicle.Storage.Sql.for_Database.when_wiping;

public class and_a_sqlite_migration_is_in_flight : given.a_sqlite_database
{
    readonly given.a_paused_migration _pause = new();
    bool _wipeWaited;
    string[] _pending;

    async Task Because()
    {
        _ = StartMigration(interceptors: _pause);
        try
        {
            await _pause.Entered;
            var wipe = Track(_database.Wipe());
            _wipeWaited = !wipe.IsCompleted;
        }
        finally
        {
            _pause.Release();
            await CompleteOperations();
        }

        // Reuse the same cache key: a post-wipe request must not skip migration.
        await StartMigration().WaitAsync(Deadline);
        _pending = await PendingMigrations();
    }

    [Fact] void should_wait_for_the_migration() => _wipeWaited.ShouldBeTrue();
    [Fact] void should_allow_the_next_migration_to_complete() => _pending.ShouldBeEmpty();

    void Destroy() => _pause.Release();
}
#endif
