// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator;

public class when_a_stale_lock_exists : given.a_migrator
{
    EncryptionKeyMigrationReport _report;

    void Establish()
    {
        _store.Put(VaultPathSegments.LayoutMarker, new Dictionary<string, string> { ["format"] = "k1" });
        _store.Put(VaultKeyStoreMigrator.MigrationLockPath, new Dictionary<string, string> { ["owner"] = "old-owner" });
        _store.Put($"{Address}/1", Key(1));
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_refuse_to_steal_ownership() => _report.MigrationLocked.ShouldBeTrue();
    [Fact] void should_retain_the_stale_owner_for_operator_review() => _store.DataOf(VaultKeyStoreMigrator.MigrationLockPath)["owner"].ShouldEqual("old-owner");
    [Fact] void should_not_copy() => _store.Has($"{Current}/1").ShouldBeFalse();
    [Fact] void should_not_remove_the_source() => _store.Has($"{Address}/1").ShouldBeTrue();
}
