// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path.and_the_identifier_is_plain_ascii.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path;

[Collection(VaultCollection.Name)]
public class and_the_identifier_is_plain_ascii(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey ExistingKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey NewKey = new([7, 8, 9], [10, 11, 12]);
        public EncryptionKeyAtRevision? Latest;
        public bool SavedAtRevisionTwo;
        public bool EarlierGoneAfterDelete;
        public bool NewGoneAfterDelete;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var raw = new given.raw_vault(Fixture.VaultAddress);
            await raw.WriteKey($"{_eventStore}/default/customer-42/1", ExistingKey);

            Latest = await _storage.TryGetLatestFor(_eventStore, _namespace, "customer-42");
            await _storage.SaveFor(_eventStore, _namespace, "customer-42", NewKey);
            SavedAtRevisionTwo = await raw.Exists($"k1/{_eventStore}/default/customer-42/2");
            await _storage.DeleteFor(_eventStore, _namespace, "customer-42");
            EarlierGoneAfterDelete = !await raw.Exists($"{_eventStore}/default/customer-42/1");
            NewGoneAfterDelete = !await raw.Exists($"k1/{_eventStore}/default/customer-42/2");
        }
    }

    [Fact] void should_read_it_through_the_earlier_path() => ctx.Latest!.Key.Fingerprint.ShouldEqual(ctx.ExistingKey.Fingerprint);
    [Fact] void should_save_the_next_revision_under_its_own_root() => ctx.SavedAtRevisionTwo.ShouldBeTrue();
    [Fact] void should_remove_the_earlier_secret_when_deleting() => ctx.EarlierGoneAfterDelete.ShouldBeTrue();
    [Fact] void should_remove_the_new_secret_when_deleting() => ctx.NewGoneAfterDelete.ShouldBeTrue();
}
