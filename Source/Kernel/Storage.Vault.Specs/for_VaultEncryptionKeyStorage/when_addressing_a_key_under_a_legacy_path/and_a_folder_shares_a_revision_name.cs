// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path.and_a_folder_shares_a_revision_name.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path;

[Collection(VaultCollection.Name)]
public class and_a_folder_shares_a_revision_name(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey ExistingKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey DeeperKey = new([7, 8, 9], [10, 11, 12]);
        public EncryptionKeyAtRevision? Latest;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var raw = new given.raw_vault(Fixture.VaultAddress);
            await raw.WriteKey($"{_eventStore}/default/c/1", ExistingKey);
            await raw.WriteKey($"{_eventStore}/default/c/7/1", DeeperKey);

            Latest = await _storage.TryGetLatestFor(_eventStore, _namespace, "c");
        }
    }

    [Fact] void should_read_the_key_of_the_address() => ctx.Latest!.Key.Fingerprint.ShouldEqual(ctx.ExistingKey.Fingerprint);
    [Fact] void should_not_count_the_folder_as_a_revision() => ctx.Latest!.Revision.Value.ShouldEqual(1u);
}
