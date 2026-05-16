// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_checking_if_key_exists.and_key_exists.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_checking_if_key_exists;

[Collection(VaultCollection.Name)]
public class and_key_exists(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKeyIdentifier Identifier;
        public bool Result;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Identifier = Guid.NewGuid().ToString();
            await _storage.SaveFor(_eventStore, _namespace, Identifier, new EncryptionKey([1, 2, 3], [4, 5, 6]));
            Result = await _storage.HasFor(_eventStore, _namespace, Identifier);
        }
    }

    [Fact] void should_return_true() => ctx.Result.ShouldBeTrue();
}
