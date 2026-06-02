// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_checking_if_key_exists.and_key_does_not_exist.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_checking_if_key_exists;

[Collection(VaultCollection.Name)]
public class and_key_does_not_exist(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKeyIdentifier Identifier;
        public bool Result;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Identifier = Guid.NewGuid().ToString();
            Result = await _storage.HasFor(_eventStore, _namespace, Identifier);
        }
    }

    [Fact] void should_return_false() => ctx.Result.ShouldBeFalse();
}
