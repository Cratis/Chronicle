// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Security.KeyVault.Secrets;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_a_key_secret_is_disabled.and_the_erasure_is_disabled.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_a_key_secret_is_disabled;

[Collection(AzureKeyVaultCollection.Name)]
public class and_the_erasure_is_disabled(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        const string Identifier = "customer-42";

        public EncryptionKey ExistingKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey FreshKey = new([7, 8, 9], [10, 11, 12]);
        public Exception? Error;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var locations = SecretLocations.For(_eventStore, _namespace, Identifier);
            var erasure = new KeyVaultSecret(locations.ErasureName, "{\"erasedThrough\":\"1\",\"erasedKeyFingerprints\":\"\",\"newKeyAllowed\":\"True\"}");
            erasure.Properties.Enabled = false;
            await Fixture.SecretClient.SetSecretAsync(erasure);
            Error = await Catch.Exception(() => _storage.GetOrAddFor(_eventStore, _namespace, Identifier, FreshKey));
        }
    }

    [Fact] void should_report_the_key_as_unavailable() => ctx.Error.ShouldBeOfExactType<EncryptionKeyUnavailable>();
}
