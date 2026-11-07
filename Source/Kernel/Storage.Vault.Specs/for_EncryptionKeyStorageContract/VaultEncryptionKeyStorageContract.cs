// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using VaultSharp;

namespace Cratis.Chronicle.Storage.Vault.for_EncryptionKeyStorageContract;

/// <summary>
/// Vault-specific ordering contracts, run against both the fake IVaultClient and the pinned CI server.
/// </summary>
[PerFactSpecificationLifecycle]
public abstract class VaultEncryptionKeyStorageContract : Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyStorageContract.EncryptionKeyStorageContract
{
    protected static readonly EncryptionKeyIdentifier Identifier = "subject";
    protected static readonly EncryptionKey Original = new([1], [2]);
    protected IVaultClient Client;
    protected IEncryptionKeyStorage Storage;
    protected given.VaultInterleaving Interleaving;
    protected string Mount = "secret";

    protected override Task DuringPublication(Func<Task> write, Func<Task> competingOperation) => Interleaving.DuringPublication(write, competingOperation);
    protected override Task DuringErasure(Func<Task> erase, Func<Task> competingOperation) => Interleaving.DuringErasure(erase, competingOperation);
    protected override Task LoseWriteResponse(Func<Task> write) => Interleaving.LoseWriteResponse(write);

    [Fact]
    public async Task should_invalidate_publication_paused_after_validation_before_destroying_its_stage()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        await Assert.ThrowsAsync<EncryptionKeyFenceChanged>(() => DuringPublication(
            () => Storage.SaveFor("contract", "contract", Identifier, new EncryptionKey([3], [4]), 99u),
            async () =>
            {
                await Storage.RecordErasureFor("contract", "contract", Identifier, new("paused"));
                await Storage.DeleteThroughFor("contract", "contract", Identifier, "paused");
                await Storage.ConfirmErasureFor("contract", "contract", Identifier, "paused");
            }));
        (await Storage.TryGetFor("contract", "contract", Identifier, 99u)).ShouldBeNull();
        var metadata = await Client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(Path(99u), mountPoint: Mount);
        metadata.Data.CurrentVersion.ShouldEqual(2);
        metadata.Data.Versions["1"].Destroyed.ShouldBeTrue();
        var tombstone = await Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(Path(99u), mountPoint: Mount);
        tombstone.Data.Data.ContainsKey("erasedTombstone").ShouldBeTrue();
        tombstone.Data.Data.ContainsKey("privateKey").ShouldBeFalse();
    }

    [Fact]
    public async Task should_model_destroy_as_retaining_current_version_and_allowing_cas_against_that_destroyed_version()
    {
        var path = Path(57u);
        var first = await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, new Dictionary<string, object> { ["test"] = true }, checkAndSet: 0, mountPoint: Mount);
        await Client.V1.Secrets.KeyValue.V2.DestroySecretVersionsAsync(path, [first.Data.Version], mountPoint: Mount);
        var metadata = await Client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(path, mountPoint: Mount);
        metadata.Data.CurrentVersion.ShouldEqual(first.Data.Version);
        var second = await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, new Dictionary<string, object> { ["test"] = true }, checkAndSet: first.Data.Version, mountPoint: Mount);
        second.Data.Version.ShouldEqual(first.Data.Version + 1);
    }

    [Fact]
    public async Task should_retry_the_sweep_when_a_publisher_wins_the_tombstone_cas()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        await Storage.RecordErasureFor("contract", "contract", Identifier, new("race"));
        await Interleaving.DuringTombstone(
            () => Storage.DeleteThroughFor("contract", "contract", Identifier, "race"),
            async () =>
            {
                var observed = await Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(Path(1u), mountPoint: Mount);
                await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(Path(1u), observed.Data.Data, checkAndSet: observed.Data.Metadata.Version, mountPoint: Mount);
            });
        await Storage.ConfirmErasureFor("contract", "contract", Identifier, "race");
        var metadata = await Client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(Path(1u), mountPoint: Mount);
        metadata.Data.CurrentVersion.ShouldEqual(4);
        metadata.Data.Versions.Where(_ => int.Parse(_.Key, CultureInfo.InvariantCulture) < metadata.Data.CurrentVersion).All(_ => _.Value.Destroyed).ShouldBeTrue();
    }

    [Fact]
    public async Task should_not_undo_a_revision_that_a_different_writer_has_published()
    {
        var winner = new EncryptionKey([7], [8]);
        await Assert.ThrowsAsync<EncryptionKeyFenceChanged>(() => DuringPublication(
            () => Storage.SaveFor("contract", "contract", Identifier, Original, 99u),
            async () =>
            {
                var staged = await Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(Path(99u), mountPoint: Mount);
                staged.Data.Data["writerNonce"] = "another-writer";
                staged.Data.Data["published"] = true;
                staged.Data.Data["publicKey"] = Convert.ToBase64String(winner.Public);
                staged.Data.Data["privateKey"] = Convert.ToBase64String(winner.Private);
                await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(Path(99u), staged.Data.Data, checkAndSet: staged.Data.Metadata.Version, mountPoint: Mount);
            }));
        (await Storage.TryGetFor("contract", "contract", Identifier, 99u))!.Fingerprint.ShouldEqual(winner.Fingerprint);
    }

    protected static string Path(EncryptionKeyRevision revision) => VaultKeyLocations.For("contract", "contract", Identifier).Path(revision);
}
