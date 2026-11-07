// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

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

    [Fact]
    public async Task should_destroy_history_before_overwriting_a_key_missed_by_the_erasure_snapshot()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        await DuringErasure(
            () => Storage.RecordErasureFor("contract", "contract", Identifier, new("snapshot")),
            () => Storage.SaveFor("contract", "contract", Identifier, new EncryptionKey([3], [4]), 99u));
        var erased = (await Storage.GetFenceFor("contract", "contract", Identifier))!;
        erased.ErasedThrough.Value.ShouldEqual(1u);
        await Storage.AllowNewKeyFor("contract", "contract", Identifier, erased.Version, erased.Lifecycle);
        var successor = new EncryptionKey([5], [6]);
        await Storage.SaveFor("contract", "contract", Identifier, successor, 99u);
        var metadata = await Client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(Path(99u), mountPoint: Mount);
        metadata.Data.Versions["1"].Destroyed.ShouldBeTrue();
        metadata.Data.Versions["2"].Destroyed.ShouldBeTrue();
        await Storage.DeleteThroughFor("contract", "contract", Identifier, "snapshot");
        await Storage.ConfirmErasureFor("contract", "contract", Identifier, "snapshot");
        (await Storage.GetFor("contract", "contract", Identifier, 99u)).Fingerprint.ShouldEqual(successor.Fingerprint);
    }

    [Fact]
    public async Task should_verify_and_sweep_covered_history_even_under_an_uncovered_current_key()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        await DuringErasure(
            () => Storage.RecordErasureFor("contract", "contract", Identifier, new("history")),
            () => Storage.SaveFor("contract", "contract", Identifier, new EncryptionKey([3], [4]), 99u));
        var erased = (await Storage.GetFenceFor("contract", "contract", Identifier))!;
        await Storage.AllowNewKeyFor("contract", "contract", Identifier, erased.Version, erased.Lifecycle);
        var current = await Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(Path(99u), mountPoint: Mount);
        current.Data.Data["stagedLifecycle"] = erased.Lifecycle + 1;
        current.Data.Data["publicKey"] = Convert.ToBase64String(new byte[] { 5 });
        current.Data.Data["privateKey"] = Convert.ToBase64String(new byte[] { 6 });

        // Simulate history left by an earlier implementation, bypassing guarded replacement.
        var successor = await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(Path(99u), current.Data.Data, checkAndSet: current.Data.Metadata.Version, mountPoint: Mount);
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.ConfirmErasureFor("contract", "contract", Identifier, "history"));
        await Storage.DeleteThroughFor("contract", "contract", Identifier, "history");
        await Storage.ConfirmErasureFor("contract", "contract", Identifier, "history");
        var metadata = await Client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(Path(99u), mountPoint: Mount);
        metadata.Data.Versions["1"].Destroyed.ShouldBeTrue();
        metadata.Data.Versions["2"].Destroyed.ShouldBeTrue();
        metadata.Data.Versions[successor.Data.Version.ToString(CultureInfo.InvariantCulture)].Destroyed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_read_the_requested_historical_version_instead_of_current()
    {
        var path = Path(57u);
        await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, new Dictionary<string, object> { ["value"] = "first" }, checkAndSet: 0, mountPoint: Mount);
        await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, new Dictionary<string, object> { ["value"] = "second" }, checkAndSet: 1, mountPoint: Mount);
        var historical = await Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, version: 1, mountPoint: Mount);
        historical.Data.Metadata.Version.ShouldEqual(1);
        historical.Data.Data["value"].ToString().ShouldEqual("first");
        (await Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: Mount)).Data.Data["value"].ToString().ShouldEqual("second");
    }

    [Fact]
    public async Task should_model_soft_delete_as_reversible_and_distinct_from_destruction()
    {
        var path = Path(57u);
        await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, new Dictionary<string, object> { ["value"] = "first" }, checkAndSet: 0, mountPoint: Mount);
        await Client.V1.Secrets.KeyValue.V2.DeleteSecretVersionsAsync(path, [1], mountPoint: Mount);
        await Assert.ThrowsAsync<VaultApiException>(() => Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, version: 1, mountPoint: Mount));
        var deleted = await Client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(path, mountPoint: Mount);
        deleted.Data.CurrentVersion.ShouldEqual(1);
        deleted.Data.Versions["1"].Destroyed.ShouldBeFalse();
        await Client.V1.Secrets.KeyValue.V2.UndeleteSecretVersionsAsync(path, [1], mountPoint: Mount);
        (await Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, version: 1, mountPoint: Mount)).Data.Data["value"].ToString().ShouldEqual("first");
        await Client.V1.Secrets.KeyValue.V2.DestroySecretVersionsAsync(path, [1], mountPoint: Mount);
        await Client.V1.Secrets.KeyValue.V2.UndeleteSecretVersionsAsync(path, [1], mountPoint: Mount);
        await Assert.ThrowsAsync<VaultApiException>(() => Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, version: 1, mountPoint: Mount));
    }

    [Fact]
    public async Task should_require_cas_when_the_mount_is_configured_to_require_it()
    {
        await Client.V1.Secrets.KeyValue.V2.ConfigureAsync(new KeyValue2ConfigModel { CASRequired = true }, mountPoint: Mount);
        await Assert.ThrowsAsync<VaultApiException>(() => Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(Path(57u), new Dictionary<string, object> { ["value"] = true }, mountPoint: Mount));
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        await Storage.RecordErasureFor("contract", "contract", Identifier, new("cas"));
        await Storage.DeleteThroughFor("contract", "contract", Identifier, "cas");
        await Storage.ConfirmErasureFor("contract", "contract", Identifier, "cas");
        (await Storage.GetFenceFor("contract", "contract", Identifier))!.Requests!.Single().Confirmed.ShouldBeTrue();
    }

    protected static string Path(EncryptionKeyRevision revision) => VaultKeyLocations.For("contract", "contract", Identifier).Path(revision);
}
