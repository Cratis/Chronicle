// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_run_twice : given.a_migrator
{
    EncryptionKeyMigrationReport _second;
    int _writesAfterFirst;

    async Task Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.Put($"{Address}/2", Key(2));
        await Migrate();
        _writesAfterFirst = _store.Writes;
    }

    async Task Because() => _second = await Migrate();

    [Fact] void should_find_everything_already_migrated() => _second.AlreadyMigrated.ShouldEqual(2L);
    [Fact] void should_copy_nothing() => _second.Copied.ShouldEqual(0L);
    [Fact] void should_only_write_the_lock() => _store.Writes.ShouldEqual(_writesAfterFirst + 1);
    [Fact] void should_verify_what_is_there() => _second.Verified.ShouldEqual(2L);
    [Fact] void should_leave_nothing_for_review() => _second.NeedsReview.ShouldBeFalse();
}
