// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_saving_a_key_with_an_explicit_revision.and_retrieving_by_that_revision.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_saving_a_key_with_an_explicit_revision;

[Collection(VaultCollection.Name)]
public class and_retrieving_by_that_revision(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKeyIdentifier Identifier;
        public EncryptionKey Key;
        public EncryptionKeyRevision Revision;
        public EncryptionKey? Result;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Identifier = Guid.NewGuid().ToString();
            Revision = 42u;
            Key = new EncryptionKey([10, 20, 30], [40, 50, 60]);
            await _storage.SaveFor(_eventStore, _namespace, Identifier, Key, Revision);
            Result = await _storage.GetFor(_eventStore, _namespace, Identifier, Revision);
        }
    }

    [Fact] void should_return_the_correct_public_key() => ctx.Result!.Public.ShouldEqual(ctx.Key.Public);
    [Fact] void should_return_the_correct_private_key() => ctx.Result!.Private.ShouldEqual(ctx.Key.Private);
}
