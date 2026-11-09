// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_all_versions_are_tombstones(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    EncryptionKeyMigrationReport _report = default!;

    async Task Establish()
    {
        await Put(Legacy);
        await Put(Legacy, Key(2));
        await DeleteVersions(Legacy, 1, 2);
        await Put("store/default/destroyed/1");
        await DestroyVersions("store/default/destroyed/1", 1);
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_not_copy_soft_deleted_or_destroyed_history() => _report.Scanned.ShouldEqual(0L);
    [Fact] async Task should_confirm_the_soft_deleted_tombstone() => (await _store.IsTombstone(Legacy)).ShouldBeTrue();
    [Fact] async Task should_confirm_the_destroyed_tombstone() => (await _store.IsTombstone("store/default/destroyed/1")).ShouldBeTrue();
    [Fact] async Task should_not_resurrect_soft_deleted_material() => (await _store.Read(Current) is null).ShouldBeTrue();
    [Fact] async Task should_not_resurrect_destroyed_material() => (await _store.Read("k1/store/default/destroyed/1") is null).ShouldBeTrue();
}
