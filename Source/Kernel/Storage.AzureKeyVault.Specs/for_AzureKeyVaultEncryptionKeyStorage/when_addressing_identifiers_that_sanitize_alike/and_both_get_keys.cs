// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_identifiers_that_sanitize_alike.and_both_get_keys.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_identifiers_that_sanitize_alike;

[Collection(AzureKeyVaultCollection.Name)]
public class and_both_get_keys(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey FirstKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey SecondKey = new([7, 8, 9], [10, 11, 12]);
        public EncryptionKey? FirstStored;
        public EncryptionKey? SecondStored;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            FirstStored = await _storage.GetOrAddFor(_eventStore, _namespace, "Alice.X", FirstKey);
            SecondStored = await _storage.GetOrAddFor(_eventStore, _namespace, "alice-x", SecondKey);
        }
    }

    [Fact] void should_return_the_first_key_for_the_first_identifier() => ctx.FirstStored!.Fingerprint.ShouldEqual(ctx.FirstKey.Fingerprint);
    [Fact] void should_return_the_second_key_for_the_second_identifier() => ctx.SecondStored!.Fingerprint.ShouldEqual(ctx.SecondKey.Fingerprint);
}
