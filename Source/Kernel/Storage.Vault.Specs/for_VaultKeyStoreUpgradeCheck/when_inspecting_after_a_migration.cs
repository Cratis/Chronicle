// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreMigrator.given;

namespace Cratis.Chronicle.Storage.Vault.for_VaultKeyStoreUpgradeCheck;

public class when_inspecting_after_a_migration : Specification
{
    InMemoryVaultSecretStore _store;
    EncryptionKeyStoreUpgradeStatus _status;

    void Establish()
    {
        static Dictionary<string, string> key(byte publicByte) => new()
        {
            ["publicKey"] = Convert.ToBase64String([publicByte]),
            ["privateKey"] = Convert.ToBase64String([publicByte, 1])
        };

        _store = new();
        _store.Put(VaultPathSegments.LayoutMarker, new Dictionary<string, string> { ["format"] = "k1" });
        _store.Put("store/default/customer-42/1", key(1));
        _store.Put("k1/store/default/customer-42/1", new Dictionary<string, string>(key(1)) { ["layout"] = "k1" });
        _store.Put("store/default/customer-42/2", key(2));
        _store.Put("k1/store/default/customer-42/2", new Dictionary<string, string>(key(9)) { ["layout"] = "k1" });
        _store.Put("store/default/customer-43/1", key(3));
    }

    async Task Because() => _status = await new VaultKeyStoreUpgradeCheck(_store).Inspect(true);

    [Fact] void should_not_count_what_is_already_migrated_as_pending() => _status.MigratedLegacyRetained.ShouldEqual(1L);
    [Fact] void should_count_a_copy_that_differs_as_pending() => _status.EarlierNameKeys.ShouldEqual(2L);
    [Fact] void should_still_have_material_pending() => _status.EarlierMaterialPresent.ShouldBeTrue();
}
