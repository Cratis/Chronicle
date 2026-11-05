// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path.and_an_escaped_identifier_spells_out_a_path.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path;

[Collection(VaultCollection.Name)]
public class and_an_escaped_identifier_spells_out_a_path(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey ExistingKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey FreshKey = new([7, 8, 9], [10, 11, 12]);
        public EncryptionKeyAtRevision? OtherLatest;
        public EncryptionKeyAtRevision? EscapedLatest;
        public bool NothingAtTheRawPath;
        public EncryptionKey? Provisioned;
        public bool NewLocationWritten;
        public EncryptionKey? ExistingAfterDeletes;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var raw = new given.raw_vault(Fixture.VaultAddress);
            await raw.WriteKey($"{_eventStore}/default/a~2Fb/1", ExistingKey);

            OtherLatest = await _storage.TryGetLatestFor(_eventStore, _namespace, "a/b");
            NothingAtTheRawPath = !await raw.Exists($"{_eventStore}/default/a/b/1");
            EscapedLatest = await _storage.TryGetLatestFor(_eventStore, _namespace, "a~2Fb");
            Provisioned = await _storage.GetOrAddFor(_eventStore, _namespace, "a/b", FreshKey);
            NewLocationWritten = await raw.Exists($"k1/{_eventStore}/default/a~2Fb/1");
            await _storage.DeleteFor(_eventStore, _namespace, "a/b");
            await _storage.DeleteFor(_eventStore, _namespace, "a/b", 1u);
            ExistingAfterDeletes = await _storage.TryGetFor(_eventStore, _namespace, "a~2Fb");
        }
    }

    [Fact] void should_not_read_the_key_of_the_identifier_that_spells_out_the_escape() => ctx.OtherLatest.ShouldBeNull();
    [Fact] void should_not_store_anything_at_the_raw_path() => ctx.NothingAtTheRawPath.ShouldBeTrue();
    [Fact] void should_read_the_key_for_the_identifier_it_belongs_to() => ctx.EscapedLatest!.Key.Fingerprint.ShouldEqual(ctx.ExistingKey.Fingerprint);
    [Fact] void should_provision_the_given_key() => ctx.Provisioned!.Fingerprint.ShouldEqual(ctx.FreshKey.Fingerprint);
    [Fact] void should_write_it_under_its_own_root() => ctx.NewLocationWritten.ShouldBeTrue();
    [Fact] void should_leave_the_other_key_when_deleting() => ctx.ExistingAfterDeletes!.Fingerprint.ShouldEqual(ctx.ExistingKey.Fingerprint);
}
