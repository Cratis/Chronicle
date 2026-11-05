// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.and_an_earlier_erasure_is_not_canonical.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;

[Collection(AzureKeyVaultCollection.Name)]
public class and_an_earlier_erasure_is_not_canonical(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public Exception? ProvisionError;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var erasedKey = new EncryptionKey([1, 2, 3], [4, 5, 6]);
            await LegacySecrets.WriteErasure(Fixture.SecretClient, LegacySecrets.Name(_eventStore, "alice-x", "erasure"), erasedKey);

            ProvisionError = await Catch.Exception(() => _storage.GetOrAddFor(_eventStore, _namespace, "Alice.X", erasedKey));
        }
    }

    [Fact] void should_refuse_provisioning() => ctx.ProvisionError.ShouldBeOfExactType<EncryptionKeyNeedsUpgrade>();
}
