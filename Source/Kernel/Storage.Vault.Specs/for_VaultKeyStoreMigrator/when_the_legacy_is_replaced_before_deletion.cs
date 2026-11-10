// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_the_legacy_is_replaced_before_deletion : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.BeforeRemove = path =>
        {
            if (path == $"{Address}/1") _store.Put(path, Key(9));
        };
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_preserve_the_replacement() => _store.DataOf($"{Address}/1")["publicKey"].ShouldEqual(Key(9)["publicKey"]);
    [Fact] void should_keep_the_destination() => _store.Has($"{Current}/1").ShouldBeTrue();
    [Fact] void should_report_the_interference() => _report.ConflictsKept.ShouldEqual(1L);
    [Fact] void should_report_no_legacy_removals() => _report.LegacyRemoved.ShouldEqual(0L);
}
