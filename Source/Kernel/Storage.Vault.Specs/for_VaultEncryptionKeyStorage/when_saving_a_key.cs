// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_saving_a_key.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage;

[Collection(VaultCollection.Name)]
public class when_saving_a_key(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKeyIdentifier Identifier;
        public EncryptionKey Key;
        public bool HasKey;
        public EncryptionKey RetrievedKey;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Identifier = Guid.NewGuid().ToString();
            Key = new EncryptionKey([1, 2, 3], [4, 5, 6]);
            await _storage.SaveFor(_eventStore, _namespace, Identifier, Key);
            HasKey = await _storage.HasFor(_eventStore, _namespace, Identifier);
            RetrievedKey = await _storage.GetFor(_eventStore, _namespace, Identifier);
        }
    }

    [Fact] void should_report_that_it_has_the_key() => ctx.HasKey.ShouldBeTrue();
    [Fact] void should_return_the_correct_public_key() => ctx.RetrievedKey.Public.ShouldEqual(ctx.Key.Public);
    [Fact] void should_return_the_correct_private_key() => ctx.RetrievedKey.Private.ShouldEqual(ctx.Key.Private);
}
