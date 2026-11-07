// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreUpgradeCheck.when_inspecting.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultKeyStoreUpgradeCheck;

[Collection(AzureKeyVaultCollection.Name)]
public class when_inspecting(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : for_AzureKeyVaultEncryptionKeyStorage.given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public EncryptionKeyStoreUpgradeStatus? Status;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var earlier = for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.LegacySecrets.Name(_eventStore, "customer-42", "1");
            await for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.LegacySecrets.WriteKey(Fixture.SecretClient, earlier, new EncryptionKey([1, 2, 3], [4, 5, 6]));
            await for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.LegacySecrets.WriteErasure(Fixture.SecretClient, for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.LegacySecrets.Name(_eventStore, "customer-42", "erasure"), new EncryptionKey([1, 2, 3], [4, 5, 6]));
            await _storage.SaveFor(_eventStore, _namespace, "customer-42", new EncryptionKey([7, 8, 9], [10, 11, 12]));

            Status = await new AzureKeyVaultKeyStoreUpgradeCheck(Fixture.SecretClient).Inspect(true);
        }
    }

    [Fact] void should_count_at_least_the_earlier_key() => ctx.Status!.EarlierNameKeys.ShouldBeGreaterThanOrEqual(1L);
    [Fact] void should_count_at_least_the_earlier_erasure() => ctx.Status!.EarlierNameErasures.ShouldBeGreaterThanOrEqual(1L);
    [Fact] void should_report_no_unresolved_shapes() => ctx.Status!.UnresolvedShapes.ShouldEqual(0L);
    [Fact] void should_have_a_usable_layout() => ctx.Status!.LayoutUnusable.ShouldBeFalse();
    [Fact] void should_be_complete() => ctx.Status!.Complete.ShouldBeTrue();
    [Fact] void should_report_material_present() => ctx.Status!.EarlierMaterialPresent.ShouldBeTrue();
}
