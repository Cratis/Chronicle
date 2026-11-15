// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.and_a_legacy_erasure_exists.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;

[Collection(AzureKeyVaultCollection.Name)]
public class and_a_legacy_erasure_exists(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public EncryptionKeyErasure? Erasure;
        public Exception? ProvisioningError;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            EncryptionKeyIdentifier identifier = "customer-42";
            var erasedKey = new EncryptionKey([1, 2, 3], [4, 5, 6]);
            await LegacySecrets.WriteErasure(Fixture.SecretClient, LegacySecrets.Name(_eventStore, identifier, "erasure"), erasedKey);

            Erasure = await _storage.GetErasureFor(_eventStore, _namespace, identifier);
            ProvisioningError = await Catch.Exception(() => _storage.GetOrAddFor(_eventStore, _namespace, identifier, erasedKey));
        }
    }

    [Fact] void should_read_the_earlier_erasure() => ctx.Erasure!.ErasedThrough.Value.ShouldEqual(1u);
    [Fact] void should_refuse_to_provision_the_erased_key_again() => ctx.ProvisioningError.ShouldBeOfExactType<EncryptionKeyErased>();
}
