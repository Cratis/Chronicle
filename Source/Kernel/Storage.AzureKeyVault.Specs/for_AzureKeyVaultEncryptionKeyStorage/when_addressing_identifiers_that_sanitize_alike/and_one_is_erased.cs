// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_identifiers_that_sanitize_alike.and_one_is_erased.context;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_AzureKeyVaultEncryptionKeyStorage.when_addressing_identifiers_that_sanitize_alike;

[Collection(AzureKeyVaultCollection.Name)]
public class and_one_is_erased(context ctx) : IClassFixture<context>
{
    public class context(AzureKeyVaultFixture fixture) : given.a_azure_key_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey OtherKey = new([7, 8, 9], [10, 11, 12]);
        public EncryptionKeyAtRevision? ErasedLatest;
        public EncryptionKeyAtRevision? OtherLatest;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            await _storage.SaveFor(_eventStore, _namespace, "Alice.X", new EncryptionKey([1, 2, 3], [4, 5, 6]));
            await _storage.SaveFor(_eventStore, _namespace, "alice-x", OtherKey);
            await _storage.DeleteFor(_eventStore, _namespace, "Alice.X");
            ErasedLatest = await _storage.TryGetLatestFor(_eventStore, _namespace, "Alice.X");
            OtherLatest = await _storage.TryGetLatestFor(_eventStore, _namespace, "alice-x");
        }
    }

    [Fact] void should_remove_the_erased_identifiers_key() => ctx.ErasedLatest.ShouldBeNull();
    [Fact] void should_keep_the_other_identifiers_key() => ctx.OtherLatest!.Key.Fingerprint.ShouldEqual(ctx.OtherKey.Fingerprint);
}
