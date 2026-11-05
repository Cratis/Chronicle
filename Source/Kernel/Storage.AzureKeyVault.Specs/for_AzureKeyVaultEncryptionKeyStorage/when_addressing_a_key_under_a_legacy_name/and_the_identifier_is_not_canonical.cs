// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.and_the_identifier_is_not_canonical.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;

[Collection(AzureKeyVaultCollection.Name)]
public class and_the_identifier_is_not_canonical(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public EncryptionKeyAtRevision? Latest;
        public bool LegacyIsLive;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var legacyName = LegacySecrets.Name(_eventStore, "alice-x", "1");
            await LegacySecrets.WriteKey(Fixture.SecretClient, legacyName, new EncryptionKey([1, 2, 3], [4, 5, 6]));

            Latest = await _storage.TryGetLatestFor(_eventStore, _namespace, "Alice.X");
            await _storage.DeleteFor(_eventStore, _namespace, "Alice.X");
            LegacyIsLive = await LegacySecrets.IsLive(Fixture.SecretClient, legacyName);
        }
    }

    [Fact] void should_not_read_the_earlier_secret() => ctx.Latest.ShouldBeNull();
    [Fact] void should_not_remove_the_earlier_secret() => ctx.LegacyIsLive.ShouldBeTrue();
}
