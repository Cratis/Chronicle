// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EncryptionKeyStorageContract;

public class when_persisting_the_authority : given.a_fenced_store
{
    [Fact]
    public void should_round_trip_publication_and_import_metadata_with_the_fence()
    {
        var state = new EncryptionKeyFenceState(
            new(7u, ["old"], false, 9, 3, 3, [new("erase", 7u, 3, ["old"])]),
            [new(8, "name", "exact-version", 3, "new")],
            [new("legacy", "imported-version", DateTimeOffset.UnixEpoch, 3, ["imported-version"])]);
        var json = state.Serialize();
        var decoded = EncryptionKeyFenceState.Read(Identifier, 9, json, EncryptionKeyFenceState.Seal(json));
        decoded.Erasure.ErasedThrough.Value.ShouldEqual(7u);
        decoded.Publications.Single().Version.ShouldEqual("exact-version");
        decoded.LegacyImports.Single().Version.ShouldEqual("imported-version");
        decoded.Erasure.Requests!.Single().RequestId.ShouldEqual("erase");
    }

    [Fact]
    public void should_reject_an_unsealed_old_shape_authority_row()
    {
        var json = EncryptionKeyFenceState.Empty.Serialize();
        Assert.Throws<EncryptionKeyFenceUnavailable>(() => EncryptionKeyFenceState.Read(Identifier, 0, json, string.Empty));
    }

    [Fact]
    public async Task should_include_every_version_fingerprint_in_the_erasure_boundary()
    {
        await Storage.SaveFor(EventStore, Namespace, Identifier, Original, 1u);
        await Storage.SaveFor(EventStore, Namespace, Identifier, Successor, 1u);
        await Storage.RecordErasureFor(EventStore, Namespace, Identifier, new("all-versions"));
        var fence = (await Fence())!;
        fence.ErasedKeyFingerprints.ShouldContain(Original.Fingerprint);
        fence.ErasedKeyFingerprints.ShouldContain(Successor.Fingerprint);
        await Storage.DeleteThroughFor(EventStore, Namespace, Identifier, "all-versions");
        await Storage.ConfirmErasureFor(EventStore, Namespace, Identifier, "all-versions");
        await Authorize();
        await Assert.ThrowsAsync<EncryptionKeyErased>(() => Storage.SaveFor(EventStore, Namespace, Identifier, Original, 99u));
    }
}
