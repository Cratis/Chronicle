// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault.for_EncryptionKeyStorageContract;

public class on_fake : VaultEncryptionKeyStorageContract
{
    given.FakeVault _fake;

    protected override IEncryptionKeyStorage CreateStorage()
    {
        _fake = new();
        _fake.Seed(VaultPathSegments.LayoutMarker, new() { ["format"] = VaultPathSegments.Root });
        Interleaving = new(_fake);
        Client = given.FakeVault.Client(Interleaving);
        return Storage = new VaultEncryptionKeyStorage(Client, random: new Random(17), delay: _ => Task.CompletedTask);
    }

    [Fact]
    public async Task should_bound_conflict_retries_with_injected_jittered_backoff()
    {
        var delays = new List<TimeSpan>();
        var storage = new VaultEncryptionKeyStorage(Client, random: new Random(17), delay: delay =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        });
        _fake.StageConflicts = 20;
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => storage.SaveFor("contract", "contract", Identifier, Original));
        delays.Count.ShouldEqual(8);
        delays.TrueForAll(_ => _.TotalMilliseconds is >= 1 and < 64).ShouldBeTrue();
        var random = new Random(17);
        Enumerable.Range(0, 32).Select(_ => VaultFenceAccess.BackoffDelay(5, random)).Distinct().Count().ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task should_preserve_an_existing_request_hook()
    {
        var invoked = false;
        Client.Settings.BeforeApiRequestAction = (_, _) => invoked = true;
        var storage = new VaultEncryptionKeyStorage(Client);
        await storage.SaveFor("contract", "contract", Identifier, Original);
        invoked.ShouldBeTrue();
        Interleaving.SawForwardingHeader.ShouldBeTrue();
    }

    [Fact]
    public async Task should_ignore_unknown_fields_when_validating_a_sealed_fence()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        var path = VaultKeyLocations.For("contract", "contract", Identifier).ErasurePath;
        var row = await Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: Mount);
        row.Data.Data["futureField"] = "ignored";
        await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, row.Data.Data, mountPoint: Mount);
        (await Storage.GetFenceFor("contract", "contract", Identifier))!.Seal.ShouldNotBeNull();
    }

    [Fact]
    public async Task should_read_an_old_fence_and_upgrade_it_on_the_first_guarded_write()
    {
        var path = VaultKeyLocations.For("contract", "contract", Identifier).ErasurePath;
        _fake.Seed(path, new() { ["erasedThrough"] = "2", ["erasedKeyFingerprints"] = string.Empty, ["newKeyAllowed"] = "False", ["unknownFutureField"] = "ignored" });
        (await Storage.GetFenceFor("contract", "contract", Identifier))!.Lifecycle.ShouldEqual(1u);
        await Storage.RecordErasureFor("contract", "contract", Identifier, new("legacy"));
        var fence = (await Storage.GetFenceFor("contract", "contract", Identifier))!;
        fence.Seal.ShouldNotBeNull();
        fence.ErasedThrough.Value.ShouldEqual(2u);
        fence.Requests.ShouldContain(_ => _.RequestId == "legacy");
    }

    [Fact]
    public async Task should_read_old_key_material_without_publication_metadata()
    {
        _fake.Seed("contract/contract/subject/1", new() { ["publicKey"] = Convert.ToBase64String(Original.Public), ["privateKey"] = Convert.ToBase64String(Original.Private) });
        (await Storage.TryGetLatestFor("contract", "contract", Identifier))!.Key.Fingerprint.ShouldEqual(Original.Fingerprint);
    }

    [Fact]
    public async Task should_refuse_a_fence_with_partially_missing_protocol_fields()
    {
        var path = VaultKeyLocations.For("contract", "contract", Identifier).ErasurePath;
        _fake.Seed(path, new() { ["erasedThrough"] = "0", ["erasedKeyFingerprints"] = string.Empty, ["newKeyAllowed"] = "True", ["version"] = "1" });
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.GetFenceFor("contract", "contract", Identifier));
    }

    [Fact]
    public async Task should_refuse_an_old_writer_replacing_sealed_fence_semantics()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        var path = VaultKeyLocations.For("contract", "contract", Identifier).ErasurePath;
        var row = await Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: Mount);
        row.Data.Data["newKeyAllowed"] = "False";
        await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, row.Data.Data, mountPoint: Mount);
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.GetFenceFor("contract", "contract", Identifier));
    }

    [Fact]
    public void should_report_none_when_cas_is_ignored()
    {
        _fake.IgnoreCas = true;
        Storage.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.None);
    }

    [Fact]
    public async Task should_keep_release_n_legacy_erasure_available_when_cas_is_unsupported()
    {
        _fake.IgnoreCas = true;
        _fake.Seed("contract/contract/subject/1", new() { ["publicKey"] = Convert.ToBase64String(Original.Public), ["privateKey"] = Convert.ToBase64String(Original.Private) });
        Storage.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.None);
        const string legacyPath = "contract/contract/subject/1";
        var original = await Client.V1.Secrets.KeyValue.V2.ReadSecretAsync(legacyPath, mountPoint: Mount);
        await Client.V1.Secrets.KeyValue.V2.WriteSecretAsync(legacyPath, original.Data.Data, mountPoint: Mount);
        await Storage.RecordErasureFor("contract", "contract", Identifier);
        await Storage.DeleteFor("contract", "contract", Identifier);
        (await Storage.TryGetLatestFor("contract", "contract", Identifier)).ShouldBeNull();
        (await Storage.GetErasureFor("contract", "contract", Identifier)).ShouldNotBeNull();
        var metadata = await Client.V1.Secrets.KeyValue.V2.ReadSecretMetadataAsync(legacyPath, mountPoint: Mount);
        metadata.Data.Versions["1"].Destroyed.ShouldBeTrue();
        metadata.Data.Versions["2"].Destroyed.ShouldBeTrue();
    }

    [Fact]
    public async Task should_still_fail_closed_on_unverified_enterprise_reads_in_release_n()
    {
        _fake.Enterprise = true;
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.GetFenceFor("contract", "contract", Identifier));
    }

    [Fact]
    public async Task should_still_fail_closed_on_refused_reads_when_cas_is_unsupported()
    {
        _fake.IgnoreCas = true;
        Storage.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.None);
        _fake.RefuseReads = true;
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.GetFenceFor("contract", "contract", Identifier));
    }

    [Fact]
    public void should_report_none_for_unverified_enterprise_forwarding()
    {
        _fake.Enterprise = true;
        Storage.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.None);
    }

    [Fact]
    public async Task should_send_the_forwarding_header_on_validation_reads()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        Interleaving.SawForwardingHeader.ShouldBeTrue();
    }

    [Fact]
    public async Task should_fail_closed_when_the_validation_read_is_refused()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        _fake.RefuseReads = true;
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.GetFenceFor("contract", "contract", Identifier));
    }

    [Fact]
    public async Task should_refuse_confirmation_when_listing_is_denied()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        await Storage.RecordErasureFor("contract", "contract", Identifier, new("denied"));
        _fake.RefuseLists = true;
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.ConfirmErasureFor("contract", "contract", Identifier, "denied"));
    }

    [Fact]
    public async Task should_not_confirm_soft_deleted_key_material_as_physically_erased()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        await Storage.RecordErasureFor("contract", "contract", Identifier, new("soft"));
        await Client.V1.Secrets.KeyValue.V2.DeleteSecretVersionsAsync(Path(1u), [1, 2], mountPoint: Mount);
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.ConfirmErasureFor("contract", "contract", Identifier, "soft"));
        (await Storage.GetFenceFor("contract", "contract", Identifier))!.Requests!.Single().Confirmed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_refuse_confirmation_after_a_tombstone_whose_destroy_was_denied()
    {
        await Storage.SaveFor("contract", "contract", Identifier, Original);
        await Storage.RecordErasureFor("contract", "contract", Identifier, new("denied"));
        _fake.RefuseDestroy = true;
        await Assert.ThrowsAnyAsync<Exception>(() => Storage.DeleteThroughFor("contract", "contract", Identifier, "denied"));
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Storage.ConfirmErasureFor("contract", "contract", Identifier, "denied"));
    }
}
