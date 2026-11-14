// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreMigrator.given;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreUpgradeCheck;

public class when_inspecting_ambiguous_earlier_secrets : a_migrator
{
    EncryptionKeyStoreUpgradeStatus _full;
    EncryptionKeyStoreUpgradeStatus _quick;
    EncryptionKeyStoreUpgradeStatus _withoutScopes;

    void Establish()
    {
        _scopes.Add(new("Other", Namespace));
        _scopes.Add(new("other", Namespace));
        _vault.Put(Legacy(1), Key(1));
        _vault.Put("chronicle--other--default--customer-42--1", Key(2));
        _vault.Put("chronicle--unresolved", Key(3));
        _vault.Put("chronicle--missing--default--customer-42--erasure", Erasure(0));
        _vault.Put(Current(2), Key(2));
    }

    async Task Because()
    {
        var check = new AzureKeyVaultKeyStoreUpgradeCheck(_vault, _scopes);
        _full = await check.Inspect(true);
        _quick = await check.Inspect(false);
        _withoutScopes = await new AzureKeyVaultKeyStoreUpgradeCheck(_vault, []).Inspect(false);
    }

    [Fact] void should_report_colliding_malformed_and_unlisted_names_at_startup() => _quick.UnresolvedShapes.ShouldEqual(3L);
    [Fact] void should_report_the_same_ambiguities_during_full_inspection() => _full.UnresolvedShapes.ShouldEqual(3L);
    [Fact] void should_keep_the_earlier_key_count() => _quick.EarlierNameKeys.ShouldEqual(3L);
    [Fact] void should_keep_the_earlier_erasure_count() => _quick.EarlierNameErasures.ShouldEqual(1L);
    [Fact] void should_not_count_hashed_names_as_ambiguous() => _withoutScopes.UnresolvedShapes.ShouldEqual(4L);
    [Fact] void should_keep_ambiguous_material_pending() => _full.EarlierMaterialPresent.ShouldBeTrue();
    [Fact] void should_not_treat_ambiguous_material_as_verified() => _full.MigratedLegacyRetained.ShouldEqual(0L);
}
