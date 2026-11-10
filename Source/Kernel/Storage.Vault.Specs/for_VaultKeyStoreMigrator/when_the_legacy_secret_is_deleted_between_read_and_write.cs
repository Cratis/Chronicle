// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_the_legacy_secret_is_deleted_between_read_and_write : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.AfterWrite = path =>
        {
            if (path == $"{Current}/1")
            {
                _store.Delete($"{Address}/1");
            }
        };
    }

    async Task Because() => _report = await Migrate();

    [Fact] void should_never_roll_back_the_only_remaining_copy() => _store.Has($"{Current}/1").ShouldBeTrue();
    [Fact] void should_not_count_it_as_copied() => _report.Copied.ShouldEqual(0L);
    [Fact] void should_keep_it_for_review() => _report.ConflictsKept.ShouldEqual(1L);
}
