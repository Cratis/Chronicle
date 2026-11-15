// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator.given;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

public class when_the_mount_is_not_kv_v2 : Specification
{
    InMemoryVaultSecretStore _store;
    EncryptionKeyStoreUpgradeStatus _startup;
    EncryptionKeyStoreUpgradeStatus _inspection;
    EncryptionKeyMigrationReport _migration;

    void Establish() => _store = new InMemoryVaultSecretStore { Kv2 = false };

    async Task Because()
    {
        var check = new VaultKeyStoreUpgradeCheck(_store);
        _startup = await check.EnsureLayout();
        _inspection = await check.InspectLayout();
        _migration = await new VaultKeyStoreMigrator(_store, NullLogger<VaultKeyStoreMigrator>.Instance).Migrate(false, false);
    }

    [Fact] void should_fail_closed_on_startup() => _startup.LayoutVerified.ShouldBeFalse();
    [Fact] void should_fail_closed_on_inspection() => _inspection.LayoutVerified.ShouldBeFalse();
    [Fact] void should_refuse_migration() => _migration.LayoutUnusable.ShouldBeTrue();
    [Fact] void should_never_write() => _store.Writes.ShouldEqual(0);
}
