// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name.and_the_identifier_is_not_canonical_and_nothing_is_stored_earlier.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_name;

[Collection(AzureKeyVaultCollection.Name)]
public class and_the_identifier_is_not_canonical_and_nothing_is_stored_earlier(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey FreshKey = new([7, 8, 9], [10, 11, 12]);
        public EncryptionKey? Provisioned;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            Provisioned = await _storage.GetOrAddFor(_eventStore, _namespace, "Alice.X", FreshKey);
        }
    }

    [Fact] void should_provision_the_given_key() => ctx.Provisioned!.Fingerprint.ShouldEqual(ctx.FreshKey.Fingerprint);
}
