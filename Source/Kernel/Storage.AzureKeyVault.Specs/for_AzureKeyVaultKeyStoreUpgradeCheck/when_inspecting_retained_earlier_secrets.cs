// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator.given;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreUpgradeCheck;

public class when_inspecting_retained_earlier_secrets : a_migrator
{
    EncryptionKeyStoreUpgradeStatus _full;
    EncryptionKeyStoreUpgradeStatus _quick;

    void Establish()
    {
        _vault.Put(Legacy(1), Key(1));
        _vault.Put(Current(1), Key(1));
        _vault.Put(Legacy(2), Key(2));
        _vault.Put(Current(2), Key(9));
        _vault.Put(Legacy(3), Key(3));
        _vault.Put(LegacyErasure, Erasure(0));
        _vault.Put(CurrentErasure, Erasure(0));
    }

    async Task Because()
    {
        var check = new AzureKeyVaultKeyStoreUpgradeCheck(_vault, _scopes);
        _full = await check.Inspect(true);
        _quick = await check.Inspect(false);
    }

    [Fact] void should_count_verified_keys_as_retained() => _full.MigratedLegacyRetained.ShouldEqual(2L);
    [Fact] void should_count_only_unverified_keys_as_pending() => _full.EarlierNameKeys.ShouldEqual(2L);
    [Fact] void should_not_count_a_verified_erasure_record_as_pending() => _full.EarlierNameErasures.ShouldEqual(0L);
    [Fact] void should_still_have_earlier_material_pending() => _full.EarlierMaterialPresent.ShouldBeTrue();
    [Fact] void should_count_everything_pending_without_a_full_inspection() => _quick.EarlierNameKeys.ShouldEqual(3L);
    [Fact] void should_not_count_retained_without_a_full_inspection() => _quick.MigratedLegacyRetained.ShouldEqual(0L);
}
