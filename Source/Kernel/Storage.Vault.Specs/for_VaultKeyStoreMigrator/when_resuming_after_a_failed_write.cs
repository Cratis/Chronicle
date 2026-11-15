// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_resuming_after_a_failed_write : given.a_migrator
{
    EncryptionKeyMigrationReport _failed;
    EncryptionKeyMigrationReport _resumed;

    async Task Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.Put($"{Address}/2", Key(2));

        // The writes are the marker, lock and first key; the second key fails.
        _store.FailOnWrite = 4;
        _failed = await Migrate();
        _store.FailOnWrite = null;
    }

    async Task Because() => _resumed = await Migrate();

    [Fact] void should_report_the_failure() => _failed.Failed.ShouldEqual(1L);
    [Fact] void should_not_be_complete_when_it_failed() => _failed.Complete.ShouldBeFalse();
    [Fact] void should_find_the_first_copy_already_there() => _resumed.AlreadyMigrated.ShouldEqual(1L);
    [Fact] void should_copy_the_rest() => _resumed.Copied.ShouldEqual(1L);
    [Fact] void should_not_fail_when_resumed() => _resumed.Failed.ShouldEqual(0L);
    [Fact] void should_have_one_copy_per_revision() => _store.Paths.Count(path => path.StartsWith($"{Current}/", StringComparison.Ordinal)).ShouldEqual(2);
}
