// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_dry_running(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    string _before = default!;
    EncryptionKeyMigrationReport _report = default!;

    async Task Establish()
    {
        await Put(Legacy);
        await Put(Legacy, Key(2));
        await Put("other/default/deleted/1");
        await DeleteVersions("other/default/deleted/1", 1);
        await Put("other/default/destroyed/1");
        await DestroyVersions("other/default/destroyed/1", 1);
        _before = await Snapshot();
    }

    async Task Because() => _report = await Migrate(dryRun: true);

    [Fact] void should_count_the_live_leaf() => _report.Scanned.ShouldEqual(1L);
    [Fact] void should_count_a_proposed_copy_without_claiming_verification_or_removal() => (_report.Copied == 1 && _report.Verified == 0 && _report.LegacyRemoved == 0).ShouldBeTrue();
    [Fact] async Task should_preserve_all_payload_versions_metadata_and_root_leaves() => (_before == await Snapshot()).ShouldBeTrue();
    [Fact] async Task should_not_create_a_marker() => (await _store.Read(VaultPathSegments.LayoutMarker) is null).ShouldBeTrue();
    [Fact] async Task should_not_create_a_lock() => (await _store.Read(VaultKeyStoreMigrator.MigrationLockPath) is null).ShouldBeTrue();
}
