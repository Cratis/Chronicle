// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_a_key_secret_is_disabled.and_erasing.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_a_key_secret_is_disabled;

[Collection(AzureKeyVaultCollection.Name)]
public class and_erasing(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        const string Identifier = "customer-42";

        public EncryptionKey ExistingKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey FreshKey = new([7, 8, 9], [10, 11, 12]);
        public bool StillLive;
        public bool SoftDeleted;
        public EncryptionKeyErasure? Erasure;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var locations = SecretLocations.For(_eventStore, _namespace, Identifier);
            var keyName = locations.Name((EncryptionKeyRevision)1u);
            await _storage.SaveFor(_eventStore, _namespace, Identifier, ExistingKey);
            await DisabledSecrets.Disable(Fixture.SecretClient, keyName);
            await _storage.RecordErasureFor(_eventStore, _namespace, Identifier);
            await _storage.DeleteFor(_eventStore, _namespace, Identifier);
            StillLive = await for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.LegacySecrets.IsLive(Fixture.SecretClient, keyName);
            SoftDeleted = await for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.LegacySecrets.IsSoftDeleted(Fixture.SecretClient, keyName);
            Erasure = await _storage.GetErasureFor(_eventStore, _namespace, Identifier);
        }
    }

    [Fact] void should_remove_the_disabled_secret() => ctx.StillLive.ShouldBeFalse();
    [Fact] void should_purge_the_disabled_secret() => ctx.SoftDeleted.ShouldBeFalse();
    [Fact] void should_record_the_erasure_through_its_revision() => ctx.Erasure!.ErasedThrough.Value.ShouldBeGreaterThanOrEqual(1u);
}
