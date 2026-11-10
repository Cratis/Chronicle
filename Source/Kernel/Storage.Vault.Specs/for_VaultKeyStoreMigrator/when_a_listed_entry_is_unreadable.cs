// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_a_listed_entry_is_unreadable : given.a_migrator
{
    EncryptionKeyStoreUpgradeStatus _status;
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.UnreadablePaths.Add($"{Address}/1");
    }

    async Task Because()
    {
        _status = await new VaultKeyStoreUpgradeCheck(_store).Inspect(true);
        _report = await Migrate(remove: true);
    }

    [Fact] void should_keep_the_entry_pending() => _status.EarlierNameKeys.ShouldEqual(1L);
    [Fact] void should_require_an_upgrade() => _status.EarlierMaterialPresent.ShouldBeTrue();
    [Fact] void should_report_a_conflict() => _report.ConflictsKept.ShouldEqual(1L);
    [Fact] void should_remove_nothing() => _report.LegacyRemoved.ShouldEqual(0L);
}
