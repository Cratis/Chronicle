// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_live_VaultForwardMigration;

[Collection(LiveVaultCollection.Name)]
public class when_preserving_erasure_fences(LiveVaultFixture fixture) : given.a_live_migration(fixture)
{
    EncryptionKeyMigrationReport _report = default!;
    readonly Dictionary<string, string> _fence = new()
    {
        ["erasedThrough"] = "1",
        ["erasedKeyFingerprints"] = new EncryptionKey([2, 1, 2], []).Fingerprint,
        ["newKeyAllowed"] = "false"
    };

    async Task Establish()
    {
        await Put(Legacy);
        await Put("store/default/customer-42/2", Key(2));
        await Put("store/default/customer-42/erasure", _fence);
        await Put("other/erased/identity/erasure", _fence);
        await Put("other/erased/identity/1");
        await DeleteVersions("other/erased/identity/1", 1);
    }

    async Task Because() => _report = await Migrate(remove: true);

    [Fact] void should_copy_both_fences() => _report.ErasureRecordsCopied.ShouldEqual(2L);
    [Fact] void should_refuse_both_the_revision_floor_and_fingerprint() => _report.ConflictsKept.ShouldEqual(2L);
    [Fact] void should_not_delete_legacy_material_on_review() => _report.LegacyRemoved.ShouldEqual(0L);
    [Fact] async Task should_preserve_the_fence_payload() => (await HasPayload("k1/store/default/customer-42/erasure", WithLayout(_fence))).ShouldBeTrue();
    [Fact] async Task should_not_copy_the_fenced_key() => (await _store.Read(Current) is null).ShouldBeTrue();
    [Fact] async Task should_not_copy_the_fingerprinted_key() => (await _store.Read("k1/store/default/customer-42/2") is null).ShouldBeTrue();
    [Fact] async Task should_not_resurrect_the_erased_identity() => (await _storage.TryGetFor("other", "erased", "identity", 1) is null).ShouldBeTrue();
}
