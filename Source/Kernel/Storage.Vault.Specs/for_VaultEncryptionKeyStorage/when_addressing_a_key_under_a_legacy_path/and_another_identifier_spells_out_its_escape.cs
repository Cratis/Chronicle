// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path.and_another_identifier_spells_out_its_escape.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path;

[Collection(VaultCollection.Name)]
public class and_another_identifier_spells_out_its_escape(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey ExistingKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKey FreshKey = new([7, 8, 9], [10, 11, 12]);
        public EncryptionKeyAtRevision? OtherLatest;
        public EncryptionKeyAtRevision? SpelledOutLatest;
        public EncryptionKey? Provisioned;
        public bool NewLocationWritten;
        public bool EarlierStillHolds;
        public EncryptionKeyErasure? SpelledOutErasure;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var raw = new given.raw_vault(Fixture.VaultAddress);
            var earlierPath = $"{_eventStore}/default/a~7E/1";
            await raw.WriteKey(earlierPath, ExistingKey);

            OtherLatest = await _storage.TryGetLatestFor(_eventStore, _namespace, "a~");
            SpelledOutLatest = await _storage.TryGetLatestFor(_eventStore, _namespace, "a~7E");
            Provisioned = await _storage.GetOrAddFor(_eventStore, _namespace, "a~", FreshKey);
            NewLocationWritten = await raw.Exists($"k1/{_eventStore}/default/a~7E/1");
            await _storage.DeleteFor(_eventStore, _namespace, "a~");
            await _storage.RecordErasureFor(_eventStore, _namespace, "a~");
            EarlierStillHolds = await raw.Exists(earlierPath);
            SpelledOutErasure = await _storage.GetErasureFor(_eventStore, _namespace, "a~7E");
        }
    }

    [Fact] void should_not_read_the_other_identifiers_key() => ctx.OtherLatest.ShouldBeNull();
    [Fact] void should_read_the_key_for_the_identifier_it_belongs_to() => ctx.SpelledOutLatest!.Key.Fingerprint.ShouldEqual(ctx.ExistingKey.Fingerprint);
    [Fact] void should_provision_the_given_key() => ctx.Provisioned!.Fingerprint.ShouldEqual(ctx.FreshKey.Fingerprint);
    [Fact] void should_write_it_under_its_own_root() => ctx.NewLocationWritten.ShouldBeTrue();
    [Fact] void should_leave_the_earlier_secret_when_deleting_and_erasing() => ctx.EarlierStillHolds.ShouldBeTrue();
    [Fact] void should_not_report_an_erasure_for_the_other_identifier() => ctx.SpelledOutErasure.ShouldBeNull();
}
