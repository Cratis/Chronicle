// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance.for_EncryptionKeyStorageContract;

/// <summary>
/// Provider-independent atomic fence contract. Each fact needs a fresh store and request history.
/// </summary>
[PerFactSpecificationLifecycle]
public abstract class EncryptionKeyStorageContract : Specification
{
    static readonly EventStoreName _eventStore = "contract";
    static readonly EventStoreNamespaceName _namespace = "contract";
    static readonly EncryptionKeyIdentifier _identifier = "subject";
    static readonly EncryptionKey _original = new([1], [2]);
    static readonly EncryptionKey _successor = new([3], [4]);
    IEncryptionKeyStorage _storage;

    /// <summary>
    /// Open a fresh provider store for one contract fact.
    /// </summary>
    /// <returns>The store.</returns>
    protected abstract IEncryptionKeyStorage CreateStorage();

    /// <summary>
    /// Run a deterministic publication interleaving. Atomic stores have no stage-to-publish window, so the
    /// competing operation precedes the indivisible write. Transport-backed harnesses override this to pause
    /// after staging, run the competing operation, then resume validation/publication (never use a clock delay).
    /// </summary>
    /// <param name="write">The write to pause.</param>
    /// <param name="competingOperation">The operation while publication is paused.</param>
    /// <returns>Async task.</returns>
    protected virtual async Task DuringPublication(Func<Task> write, Func<Task> competingOperation)
    {
        await competingOperation();
        await write();
    }

    /// <summary>
    /// Interleave an erasure with a competing lifecycle change. Atomic stores finish the erasure first;
    /// transport-backed stores pause after observing the fence but before attempting its guarded update.
    /// </summary>
    /// <param name="erase">The erasure to pause.</param>
    /// <param name="competingOperation">The operation establishing the successor.</param>
    /// <returns>Async task.</returns>
    protected virtual async Task DuringErasure(Func<Task> erase, Func<Task> competingOperation)
    {
        await erase();
        await competingOperation();
    }

    /// <summary>
    /// Lose the response after a successful write. Transport-backed harnesses override this to inject
    /// the fault at their write-response boundary, after the provider has applied the operation.
    /// </summary>
    /// <param name="write">The write whose response is lost.</param>
    /// <returns>Async task that fails with a timeout after the write succeeds.</returns>
    /// <exception cref="TimeoutException">The successful write's response was lost.</exception>
    protected virtual async Task LoseWriteResponse(Func<Task> write)
    {
        await write();
        throw new TimeoutException();
    }

    void Establish() => _storage = CreateStorage();

    [Fact]
    public void should_declare_both_atomic_guarantees() =>
        _storage.FenceGuarantees.ShouldEqual(EncryptionKeyFenceGuarantees.MonotonicFence | EncryptionKeyFenceGuarantees.FencedSave);

    [Fact]
    public async Task should_ensure_a_fence_before_the_first_save()
    {
        await Save(_original);
        var fence = await Fence();
        fence.NewKeyAllowed.ShouldBeTrue();
        fence.ErasedLifecycle.ShouldEqual(0u);
        (await Latest()).StagedLifecycle.ShouldEqual(fence.Lifecycle);
        (await Erasure()).ShouldBeNull();
    }

    [Fact]
    public async Task should_not_report_erasure_when_unwrapping_fails_after_provisioning()
    {
        using var original = RSA.Create(2048);
        using var replacement = RSA.Create(2048);
        var wrapped = original.Encrypt(new byte[32], RSAEncryptionPadding.OaepSHA256);
        await Provision(new(replacement.ExportRSAPublicKey(), replacement.ExportRSAPrivateKey()));
        using var unwrapper = RSA.Create();
        unwrapper.ImportRSAPrivateKey((await Latest()).Key.Private, out _);
        Assert.ThrowsAny<CryptographicException>(() => unwrapper.Decrypt(wrapped, RSAEncryptionPadding.OaepSHA256));
        (await Erasure()).ShouldBeNull();
    }

    [Fact]
    public async Task should_report_a_missing_key_not_erasure_after_deletion_without_erasure()
    {
        await Provision(_original);
        await _storage.DeleteFor(_eventStore, _namespace, _identifier);
        (await Erasure()).ShouldBeNull();
        (await Latest()).ShouldBeNull();
        await Assert.ThrowsAsync<MissingEncryptionKey>(() => _storage.GetFor(_eventStore, _namespace, _identifier));
        (await Fence()).ErasedLifecycle.ShouldEqual(0u);
    }

    [Fact]
    public async Task should_report_recorded_erasure_even_after_a_successor_is_authorized()
    {
        await Raise("empty");
        AssertFence(await Erasure(), await Fence());
        await Authorize(await Fence());
        await Provision(_successor);
        AssertFence(await Erasure(), await Fence());
    }

    [Fact]
    public async Task should_refuse_a_delayed_save_of_destroyed_material_at_a_higher_revision()
    {
        await Save(_original);
        await Raise("erase");
        await Sweep("erase");
        await Authorize(await Fence());
        await Assert.ThrowsAsync<EncryptionKeyErased>(() => Save(_original, 99u));
    }

    [Fact]
    public async Task should_refuse_a_stalled_authorization_after_another_erasure()
    {
        await Raise("first");
        var observed = await Fence();
        await Raise("second");
        await Assert.ThrowsAsync<EncryptionKeyFenceChanged>(() => Authorize(observed));
        (await Fence()).NewKeyAllowed.ShouldBeFalse();
    }

    [Fact]
    public async Task should_accept_only_the_immediate_authorization_retry_without_another_write()
    {
        await Raise("erase");
        var observed = await Fence();
        await Authorize(observed);
        var authorized = await Fence();
        await Authorize(observed);
        AssertFence(await Fence(), authorized);
        authorized.Version.ShouldEqual(observed.Version + 1);
        authorized.Lifecycle.ShouldEqual(observed.Lifecycle + 1);
    }

    [Fact]
    public async Task should_refuse_an_authorization_retry_after_a_later_fence_write_even_when_still_allowed()
    {
        await Raise("erase");
        var observed = await Fence();
        await Authorize(observed);
        await Confirm("erase");
        await Assert.ThrowsAsync<EncryptionKeyFenceChanged>(() => Authorize(observed));
        (await Fence()).NewKeyAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task should_not_mistake_a_confirmation_for_an_authorization()
    {
        await Raise("erase");
        await Authorize(await Fence());
        var observed = await Fence();
        await Confirm("erase");
        await Assert.ThrowsAsync<EncryptionKeyFenceChanged>(() => Authorize(observed));
    }

    [Fact]
    public async Task should_require_the_observed_lifecycle_for_an_authorization_retry()
    {
        await Raise("erase");
        var observed = await Fence();
        await Authorize(observed);
        await Assert.ThrowsAsync<EncryptionKeyFenceChanged>(() => Authorize(observed with { Lifecycle = 0 }));
    }

    [Fact]
    public async Task should_refuse_revision_two_under_an_empty_subject_fence()
    {
        await Raise("empty");
        await Assert.ThrowsAsync<EncryptionKeyErased>(() => Save(_original, 2u));
        (await Latest()).ShouldBeNull();
    }

    [Fact]
    public async Task should_refuse_first_provisioning_racing_an_empty_erasure()
    {
        await Assert.ThrowsAsync<EncryptionKeyErased>(() => DuringPublication(
            async () => await Provision(_original),
            () => Raise("empty")));
        (await Latest()).ShouldBeNull();
    }

    [Fact]
    public async Task should_never_read_an_unpublished_revision()
    {
        await DuringPublication(() => Save(_original), async () =>
        {
            (await Latest()).ShouldBeNull();
            (await _storage.TryGetFor(_eventStore, _namespace, _identifier, 1u)).ShouldBeNull();
            (await _storage.HasFor(_eventStore, _namespace, _identifier)).ShouldBeFalse();
        });
        AssertKey((await Latest()).Key, _original);
    }

    [Fact]
    public async Task should_keep_get_or_add_on_the_published_revision_during_a_later_stage()
    {
        await Save(_original);
        await DuringPublication(() => Save(_successor, 2u), async () =>
        {
            AssertKey((await Provision(_successor)).Key, _original);
            AssertKey(await _storage.GetOrAddFor(_eventStore, _namespace, _identifier, _successor), _original);
        });
        AssertKey((await Latest()).Key, _successor);
    }

    [Fact]
    public async Task should_hide_covered_rows_before_the_sweep_including_get_or_add()
    {
        await Save(_original);
        await Raise("erase");
        (await Latest()).ShouldBeNull();
        (await _storage.TryGetFor(_eventStore, _namespace, _identifier, 1u)).ShouldBeNull();
        await Assert.ThrowsAsync<EncryptionKeyErased>(async () => await Provision(_successor));
        await Assert.ThrowsAsync<EncryptionKeyErased>(async () => await _storage.GetOrAddFor(_eventStore, _namespace, _identifier, _successor));
    }

    [Fact]
    public async Task should_not_sweep_an_authorized_successor_when_an_old_request_is_retried()
    {
        await Save(_original);
        await Raise("old");
        await Sweep("old");
        await Confirm("old");
        var request = (await Fence()).Requests.Single();
        await Authorize(await Fence());
        var successor = await Provision(_successor);
        var beforeRetry = await Fence();
        await Raise("old");
        await Sweep("old");
        await Confirm("old");
        var latest = await Latest();
        latest.Revision.ShouldEqual(successor.Revision);
        latest.StagedLifecycle.ShouldEqual(successor.StagedLifecycle);
        AssertKey(latest.Key, successor.Key);
        AssertFence(await Fence(), beforeRetry);
        AssertRequest((await Fence()).Requests.Single(), request);
    }

    [Fact]
    public async Task should_keep_an_old_request_after_the_inline_log_reaches_capacity()
    {
        await Raise("old");
        await Confirm("old");
        for (var index = 1; index < 1024; index++)
        {
            await Raise($"request-{index}");
        }

        var beforeRetry = await Fence();
        await Raise("old");
        AssertFence(await Fence(), beforeRetry);
        await Assert.ThrowsAsync<EncryptionKeyRequestLogFull>(() => Raise("overflow"));
        (await Fence()).Requests.Count.ShouldEqual(1024);
    }

    [Fact]
    public async Task should_refuse_a_request_bound_to_a_superseded_lifecycle()
    {
        await Raise("first");
        var observed = await Fence();
        await Authorize(observed);
        await Assert.ThrowsAsync<EncryptionKeyFenceChanged>(() => _storage.RecordErasureFor(
            _eventStore, _namespace, _identifier, new("stale", Lifecycle: observed.Lifecycle)));
    }

    [Fact]
    public async Task should_not_retarget_an_in_flight_erasure_to_an_authorized_successor()
    {
        await Save(_original);
        EncryptionKeyErasure successorFence = null;
        await DuringErasure(
            async () =>
            {
                var result = await Catch.Exception(() => _storage.RecordErasureFor(_eventStore, _namespace, _identifier));
                result?.ShouldBeOfExactType<EncryptionKeyFenceChanged>();
            },
            async () =>
            {
                await Raise("winner");
                await Sweep("winner");
                await Authorize(await Fence());
                await Provision(_successor);
                successorFence = await Fence();
            });
        AssertFence(await Fence(), successorFence);
        AssertKey((await Latest()).Key, _successor);
    }

    [Fact]
    public async Task should_verify_the_sweep_before_confirming()
    {
        await Save(_original);
        await Raise("erase");
        await Assert.ThrowsAsync<EncryptionKeyFenceUnavailable>(() => Confirm("erase"));
        (await Fence()).Requests.Single().Confirmed.ShouldBeFalse();
        await Sweep("erase");
        await Confirm("erase");
        (await Fence()).Requests.Single().Confirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task should_keep_floors_fingerprints_and_lifecycle_boundaries_monotone()
    {
        EncryptionKeyErasure? previous = null;
        for (uint index = 1; index <= 32; index++)
        {
            var key = new EncryptionKey([(byte)index], [(byte)(index + 1)]);
            await Save(key, index * 3);
            await Raise($"erase-{index}");
            var current = await Fence();
            if (previous is not null)
            {
                (current.ErasedThrough.Value >= previous.ErasedThrough.Value).ShouldBeTrue();
                (current.ErasedLifecycle >= previous.ErasedLifecycle).ShouldBeTrue();
                previous.ErasedKeyFingerprints.All(current.ErasedKeyFingerprints.Contains).ShouldBeTrue();
                (current.Version > previous.Version).ShouldBeTrue();
            }

            await Sweep($"erase-{index}");
            await Confirm($"erase-{index}");
            await Authorize(await Fence());
            previous = current;
        }
    }

    [Fact]
    public async Task should_surface_an_unknown_fence_write_outcome_and_deduplicate_the_explicit_retry()
    {
        // Model a transport losing the response after the server applied the write. A caller must see the error;
        // it may retry only with the same request id, not turn the ambiguous outcome into a fresh erasure.
        await Assert.ThrowsAsync<TimeoutException>(() => LoseWriteResponse(() => Raise("lost-response")));
        var applied = await Fence();
        await Raise("lost-response");
        AssertFence(await Fence(), applied);
    }

    [Fact]
    public async Task should_deduplicate_an_archived_id_after_a_successor_is_published()
    {
        await Save(_original);
        await Raise("archived");
        await Sweep("archived");
        await Confirm("archived");
        var request = (await Fence()).Requests.Single();
        await _storage.CompactErasureRequestsFor(_eventStore, _namespace, _identifier, retain: 0);
        (await Fence()).Requests.ShouldBeEmpty();
        AssertRequest(await _storage.GetErasureRequestFor(_eventStore, _namespace, _identifier, "archived"), request);
        await Authorize(await Fence());
        await Save(_successor);
        await Raise("archived");
        await Sweep("archived");
        await Confirm("archived");
        AssertKey((await Latest()).Key, _successor);
        (await Fence()).NewKeyAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task should_keep_unconfirmed_requests_inline_during_compaction()
    {
        await Save(_original);
        await Raise("pending");
        var request = (await Fence()).Requests.Single();
        await _storage.CompactErasureRequestsFor(_eventStore, _namespace, _identifier, retain: 0);
        AssertRequest((await Fence()).Requests.Single(), request);
    }

    static void AssertKey(EncryptionKey actual, EncryptionKey expected)
    {
        actual.Public.ShouldEqual(expected.Public);
        actual.Private.ShouldEqual(expected.Private);
    }

    static void AssertRequest(EncryptionKeyErasureRequest actual, EncryptionKeyErasureRequest expected)
    {
        actual.RequestId.ShouldEqual(expected.RequestId);
        actual.Through.ShouldEqual(expected.Through);
        actual.Lifecycle.ShouldEqual(expected.Lifecycle);
        actual.Confirmed.ShouldEqual(expected.Confirmed);
        (actual.Fingerprints ?? []).Order(StringComparer.Ordinal).ShouldEqual((expected.Fingerprints ?? []).Order(StringComparer.Ordinal));
    }

    static void AssertFence(EncryptionKeyErasure actual, EncryptionKeyErasure expected)
    {
        actual.ErasedThrough.ShouldEqual(expected.ErasedThrough);
        actual.NewKeyAllowed.ShouldEqual(expected.NewKeyAllowed);
        actual.Version.ShouldEqual(expected.Version);
        actual.Lifecycle.ShouldEqual(expected.Lifecycle);
        actual.ErasedLifecycle.ShouldEqual(expected.ErasedLifecycle);
        actual.Seal.ShouldEqual(expected.Seal);
        actual.ErasedKeyFingerprints.Order(StringComparer.Ordinal).ShouldEqual(expected.ErasedKeyFingerprints.Order(StringComparer.Ordinal));
        var requests = actual.Requests ?? [];
        var expectedRequests = expected.Requests ?? [];
        requests.Count.ShouldEqual(expectedRequests.Count);
        for (var index = 0; index < requests.Count; index++)
        {
            AssertRequest(requests[index], expectedRequests[index]);
        }
    }

    Task Save(EncryptionKey key, EncryptionKeyRevision? revision = null) => _storage.SaveFor(_eventStore, _namespace, _identifier, key, revision);
    Task<EncryptionKeyAtRevision> Provision(EncryptionKey key) => _storage.GetOrAddWithRevisionFor(_eventStore, _namespace, _identifier, key);
    Task<EncryptionKeyAtRevision?> Latest() => _storage.TryGetLatestFor(_eventStore, _namespace, _identifier);
    Task Raise(string requestId) => _storage.RecordErasureFor(_eventStore, _namespace, _identifier, new(requestId));
    Task Sweep(string requestId) => _storage.DeleteThroughFor(_eventStore, _namespace, _identifier, requestId);
    Task Confirm(string requestId) => _storage.ConfirmErasureFor(_eventStore, _namespace, _identifier, requestId);
    Task Authorize(EncryptionKeyErasure observed) => _storage.AllowNewKeyFor(_eventStore, _namespace, _identifier, observed.Version, observed.Lifecycle);
    Task<EncryptionKeyErasure?> Erasure() => _storage.GetErasureFor(_eventStore, _namespace, _identifier);
    async Task<EncryptionKeyErasure> Fence() => (await _storage.GetFenceFor(_eventStore, _namespace, _identifier))!;
}
