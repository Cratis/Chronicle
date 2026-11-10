// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator.given;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

public class when_inspecting_after_a_complete_migration : Specification
{
    InMemoryVaultSecretStore _store;
    EncryptionKeyStoreUpgradeStatus _status;

    async Task Establish()
    {
        _store = new();
        _store.Put("store/default/customer-42/1", new Dictionary<string, string> { ["publicKey"] = "AQ==", ["privateKey"] = "Ag==" });
        _store.Put("store/default/customer-42/erasure", new Dictionary<string, string> { ["erasedThrough"] = "0" });
        await new VaultKeyStoreMigrator(_store, Microsoft.Extensions.Logging.Abstractions.NullLogger<VaultKeyStoreMigrator>.Instance).Migrate(false, false);
    }

    async Task Because() => _status = await new VaultKeyStoreUpgradeCheck(_store).Inspect(true);

    [Fact] void should_count_everything_as_already_migrated() => _status.MigratedLegacyRetained.ShouldEqual(2L);
    [Fact] void should_have_nothing_pending() => _status.EarlierMaterialPresent.ShouldBeFalse();
    [Fact] void should_be_complete() => _status.Complete.ShouldBeTrue();
}
