// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_rerunning_after_cleanup : given.a_migrator
{
    EncryptionKeyMigrationReport _report;
    EncryptionKeyStoreUpgradeStatus _full;
    EncryptionKeyStoreUpgradeStatus _quick;

    async Task Establish()
    {
        _store.Put($"{Address}/1", Key(1));
        _store.Put($"{Address}/erasure", new Dictionary<string, string> { ["erasedThrough"] = "0" });
        await Migrate(remove: true);
    }

    async Task Because()
    {
        var check = new VaultKeyStoreUpgradeCheck(_store);
        _full = await check.Inspect(true);
        _quick = await check.Inspect(false);
        _report = await Migrate(remove: true);
    }

    [Fact] void should_retain_the_deleted_key_metadata() => _store.Paths.ShouldContain($"{Address}/1");
    [Fact] void should_retain_the_deleted_erasure_metadata() => _store.Paths.ShouldContain($"{Address}/erasure");
    [Fact] void should_have_no_pending_material_on_full_inspection() => _full.EarlierMaterialPresent.ShouldBeFalse();
    [Fact] void should_have_no_pending_material_on_quick_inspection() => _quick.EarlierMaterialPresent.ShouldBeFalse();
    [Fact] void should_complete_the_rerun() => _report.Complete.ShouldBeTrue();
    [Fact] void should_scan_no_legacy_entries() => _report.Scanned.ShouldEqual(0L);
    [Fact] void should_copy_nothing() => _report.Copied.ShouldEqual(0L);
    [Fact] void should_copy_no_erasure_records() => _report.ErasureRecordsCopied.ShouldEqual(0L);
    [Fact] void should_remove_nothing() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] void should_report_no_conflicts() => _report.ConflictsKept.ShouldEqual(0L);
    [Fact] void should_report_no_failures() => _report.Failed.ShouldEqual(0L);
}
