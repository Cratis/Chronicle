// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path.and_the_identifier_is_ordinary.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path;

[Collection(VaultCollection.Name)]
public class and_the_identifier_is_ordinary(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey ExistingKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKeyAtRevision? Latest;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            await new given.raw_vault(Fixture.VaultAddress).WriteKey($"{_eventStore}/default/customer-42/1", ExistingKey);
            Latest = await _storage.TryGetLatestFor(_eventStore, _namespace, "customer-42");
        }
    }

    [Fact] void should_read_the_existing_key() => ctx.Latest!.Key.Fingerprint.ShouldEqual(ctx.ExistingKey.Fingerprint);
}
