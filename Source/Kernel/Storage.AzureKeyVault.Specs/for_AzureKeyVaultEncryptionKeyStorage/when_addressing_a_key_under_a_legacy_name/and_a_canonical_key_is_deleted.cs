// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.and_a_canonical_key_is_deleted.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;

[Collection(AzureKeyVaultCollection.Name)]
public class and_a_canonical_key_is_deleted(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public EncryptionKeyAtRevision? Latest;
        public bool LegacyIsLive;
        public bool LegacyIsRecoverable;
        public bool CurrentIsLive;
        public bool CurrentIsRecoverable;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            EncryptionKeyIdentifier identifier = "customer-42";
            var legacyName = LegacySecrets.Name(_eventStore, identifier, "1");
            var currentName = SecretLocations.For(_eventStore, _namespace, identifier).Name(2u);
            await LegacySecrets.WriteKey(Fixture.SecretClient, legacyName, new EncryptionKey([1, 2, 3], [4, 5, 6]));
            await _storage.SaveFor(_eventStore, _namespace, identifier, new EncryptionKey([7, 8, 9], [10, 11, 12]));

            await _storage.DeleteFor(_eventStore, _namespace, identifier);

            Latest = await _storage.TryGetLatestFor(_eventStore, _namespace, identifier);
            LegacyIsLive = await LegacySecrets.IsLive(Fixture.SecretClient, legacyName);
            LegacyIsRecoverable = await LegacySecrets.IsSoftDeleted(Fixture.SecretClient, legacyName);
            CurrentIsLive = await LegacySecrets.IsLive(Fixture.SecretClient, currentName);
            CurrentIsRecoverable = await LegacySecrets.IsSoftDeleted(Fixture.SecretClient, currentName);
        }
    }

    [Fact] void should_leave_no_key() => ctx.Latest.ShouldBeNull();
    [Fact] void should_remove_the_earlier_secret() => ctx.LegacyIsLive.ShouldBeFalse();
    [Fact] void should_purge_the_earlier_secret() => ctx.LegacyIsRecoverable.ShouldBeFalse();
    [Fact] void should_remove_the_current_secret() => ctx.CurrentIsLive.ShouldBeFalse();
    [Fact] void should_purge_the_current_secret() => ctx.CurrentIsRecoverable.ShouldBeFalse();
}
