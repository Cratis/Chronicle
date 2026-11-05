// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;
using context = Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path.and_the_identifier_is_not_ascii.context;

namespace Cratis.Chronicle.Storage.Vault.for_VaultEncryptionKeyStorage.when_addressing_a_key_under_a_legacy_path;

[Collection(VaultCollection.Name)]
public class and_the_identifier_is_not_ascii(context ctx) : IClassFixture<context>
{
    public class context(VaultFixture fixture) : given.a_vault_encryption_key_storage(fixture)
    {
        public EncryptionKey LegacyKey = new([1, 2, 3], [4, 5, 6]);
        public EncryptionKeyAtRevision? Latest;
        public EncryptionKey? Provisioned;
        public bool LegacyExistsAfterDelete;
        public EncryptionKeyAtRevision? LatestAfterDelete;

        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();
            var raw = new given.raw_vault(Fixture.VaultAddress);
            var legacyPath = $"{_eventStore}/default/\u00D8yvind/1";
            await raw.WriteKey(legacyPath, LegacyKey);

            Latest = await _storage.TryGetLatestFor(_eventStore, _namespace, "\u00D8yvind");
            Provisioned = await _storage.GetOrAddFor(_eventStore, _namespace, "\u00D8yvind", new EncryptionKey([9, 9], [9, 9]));
            await _storage.DeleteFor(_eventStore, _namespace, "\u00D8yvind");
            LatestAfterDelete = await _storage.TryGetLatestFor(_eventStore, _namespace, "\u00D8yvind");
            LegacyExistsAfterDelete = await raw.Exists(legacyPath);
        }
    }

    [Fact] void should_read_the_earlier_key_as_the_latest() => ctx.Latest!.Key.Fingerprint.ShouldEqual(ctx.LegacyKey.Fingerprint);
    [Fact] void should_read_it_at_revision_one() => ctx.Latest!.Revision.Value.ShouldEqual(1u);
    [Fact] void should_return_it_instead_of_provisioning() => ctx.Provisioned!.Fingerprint.ShouldEqual(ctx.LegacyKey.Fingerprint);
    [Fact] void should_remove_the_earlier_secret_when_deleting() => ctx.LegacyExistsAfterDelete.ShouldBeFalse();
    [Fact] void should_leave_no_key_after_deleting() => ctx.LatestAfterDelete.ShouldBeNull();
}
