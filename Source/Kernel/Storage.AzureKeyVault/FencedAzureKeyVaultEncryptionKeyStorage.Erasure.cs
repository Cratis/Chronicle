// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

internal sealed partial class FencedAzureKeyVaultEncryptionKeyStorage
{
    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetFenceFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        (await ReadState(await AddressFor(eventStore, eventStoreNamespace, identifier))).Erasure;

    /// <inheritdoc/>
    public async Task<EncryptionKeyErasure?> GetErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var fence = await GetFenceFor(eventStore, eventStoreNamespace, identifier);
        return fence is { ErasedLifecycle: > 0 } || fence?.ErasedThrough.Value > 0 || fence?.ErasedKeyFingerprints.Count > 0 ? fence : null;
    }

    /// <inheritdoc/>
    public Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier) =>
        RecordErasureFor(eventStore, eventStoreNamespace, identifier, new(Guid.NewGuid().ToString("N")));

    /// <inheritdoc/>
    public async Task RecordErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyErasureRequest request)
    {
        var address = await AddressFor(eventStore, eventStoreNamespace, identifier);
        var observed = await ReadState(address);
        var lifecycle = observed.Erasure.Lifecycle;
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            if ((observed.Erasure.Requests ?? []).Any(_ => _.RequestId.Equals(request.RequestId, StringComparison.Ordinal))) return;
            if (await Archived(address, request.RequestId) is not null)
            {
                var stamped = observed with { Erasure = observed.Erasure with { Version = checked(observed.Erasure.Version + 1) } };
                if (await authority.CompareExchangeFor(eventStore, eventStoreNamespace, address.AuthorityIdentifier, observed, stamped)) return;
                await Backoff(attempt);
                observed = await ReadState(address);
                continue;
            }

            if (observed.Erasure.Lifecycle != lifecycle || (request.Lifecycle != 0 && request.Lifecycle != lifecycle)) throw new EncryptionKeyFenceChanged(identifier);
            if ((observed.Erasure.Requests?.Count ?? 0) >= 1024) throw new EncryptionKeyRequestLogFull(identifier);
            var (present, highest, fingerprints) = await Capture(address, observed);
            var covered = EncryptionKeyErasure.Covering(observed.Erasure, present);
            covered = covered with
            {
                ErasedThrough = new(Math.Max(covered.ErasedThrough.Value, highest)),
                ErasedKeyFingerprints = covered.ErasedKeyFingerprints.Concat(fingerprints).Concat(observed.Publications.Select(_ => _.Fingerprint)).Where(_ => !string.IsNullOrEmpty(_)).Distinct(StringComparer.Ordinal).ToArray()
            };
            var recorded = new EncryptionKeyErasureRequest(request.RequestId, covered.ErasedThrough, lifecycle, covered.ErasedKeyFingerprints);
            var next = observed with { Erasure = covered with { Requests = (observed.Erasure.Requests ?? []).Append(recorded).ToArray() } };
            if (await authority.CompareExchangeFor(eventStore, eventStoreNamespace, address.AuthorityIdentifier, observed, next))
            {
                await WriteLegacyBestEffort(address, next.Erasure);
                return;
            }

            await Backoff(attempt);
            observed = await ReadState(address);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        var fence = await GetFenceFor(eventStore, eventStoreNamespace, identifier);
        if (fence is { NewKeyAllowed: false }) await AllowNewKeyFor(eventStore, eventStoreNamespace, identifier, fence.Version, fence.Lifecycle);
    }

    /// <inheritdoc/>
    public async Task AllowNewKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, ulong observedVersion, uint observedLifecycle)
    {
        var address = await AddressFor(eventStore, eventStoreNamespace, identifier);
        var state = await ReadState(address);
        if (IsAuthorizationRetry(state.Erasure, observedVersion, observedLifecycle)) return;
        if (state.Erasure.NewKeyAllowed || state.Erasure.Version != observedVersion || state.Erasure.Lifecycle != observedLifecycle) throw new EncryptionKeyFenceChanged(identifier);
        var next = state with
        {
            Erasure = state.Erasure with { Version = checked(observedVersion + 1), Lifecycle = checked(observedLifecycle + 1), NewKeyAllowed = true },
            LegacyImports = state.LegacyImports.Select(_ => _ with { ClosedAtLifecycle = null }).ToArray()
        };
        if (!await authority.CompareExchangeFor(eventStore, eventStoreNamespace, address.AuthorityIdentifier, state, next))
        {
            var current = await ReadState(address);
            if (!IsAuthorizationRetry(current.Erasure, observedVersion, observedLifecycle)) throw new EncryptionKeyFenceChanged(identifier);
            return;
        }

        await WriteLegacyBestEffort(address, next.Erasure);
    }

    /// <inheritdoc/>
    public async Task DeleteFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyRevision? revision = null)
    {
        var address = await AddressFor(eventStore, eventStoreNamespace, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var state = await ReadState(address);
            var revisions = (await legacy.ListSecrets(address.Locations)).Select(_ => _.Revision.Value)
                .Concat(state.Publications.Select(_ => _.Revision)).Where(_ => IsLatest(revision) || _ == revision!.Value).Distinct().ToArray();
            var captured = revisions.Select(value => state.Publications.FirstOrDefault(_ => _.Revision == value) ??
                new EncryptionKeyPublication(value, address.Locations.Name(new(value)), string.Empty, 0, string.Empty));
            var deleted = captured.Select(_ => _ with { Deleted = true }).ToArray();
            var next = state with
            {
                Erasure = state.Erasure with { Version = checked(state.Erasure.Version + 1) },
                Publications = state.Publications.Where(_ => !revisions.Contains(_.Revision)).Concat(deleted).ToArray()
            };
            if (await authority.CompareExchangeFor(eventStore, eventStoreNamespace, address.AuthorityIdentifier, state, next))
            {
                // Raw deletion is not an erasure: hide the exact versions and disable only those captured here.
                // Name-wide irreversible deletion belongs exclusively to the bounded erasure sweep.
                foreach (var publication in deleted.Where(_ => !string.IsNullOrEmpty(_.Version))) await DisableOwnedVersion(publication, identifier);
                return;
            }

            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    /// <inheritdoc/>
    public async Task DeleteThroughFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var address = await AddressFor(eventStore, eventStoreNamespace, identifier);
        var state = await ReadState(address);
        var request = state.Erasure.Requests?.SingleOrDefault(_ => _.RequestId == requestId) ?? await Archived(address, requestId) ?? throw new EncryptionKeyFenceChanged(identifier);
        if (!request.Confirmed) await Sweep(address, request);
    }

    /// <inheritdoc/>
    public async Task ConfirmErasureFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, string requestId)
    {
        var address = await AddressFor(eventStore, eventStoreNamespace, identifier);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var state = await ReadState(address);
            var request = state.Erasure.Requests?.SingleOrDefault(_ => _.RequestId == requestId) ?? await Archived(address, requestId) ?? throw new EncryptionKeyFenceChanged(identifier);
            if (request.Confirmed) return;
            await VerifySweep(address, request);
            if (request.Confirmed) return;
            var next = state with
            {
                Erasure = state.Erasure with
                {
                    Version = checked(state.Erasure.Version + 1),
                    Requests = state.Erasure.Requests!.Select(_ => _.RequestId.Equals(requestId, StringComparison.Ordinal) ? _ with { Confirmed = true } : _).ToArray()
                }
            };
            if (await authority.CompareExchangeFor(eventStore, eventStoreNamespace, address.AuthorityIdentifier, state, next)) return;
            await Backoff(attempt);
        }

        throw new EncryptionKeyFenceUnavailable(identifier);
    }

    static bool IsAuthorizationRetry(EncryptionKeyErasure fence, ulong version, uint lifecycle) =>
        fence.NewKeyAllowed && fence.Version > version && fence.Version - version == 1 && fence.Lifecycle > lifecycle && fence.Lifecycle - lifecycle == 1;

    async Task<(List<(EncryptionKeyRevision Revision, EncryptionKey Key)> Present, uint Highest, HashSet<string> Fingerprints)> Capture(Address address, EncryptionKeyFenceState state)
    {
        var present = new List<(EncryptionKeyRevision, EncryptionKey)>();
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        var highest = 0u;
        foreach (var secret in await legacy.ListSecrets(address.Locations))
        {
            highest = Math.Max(highest, secret.Revision.Value);
            var captured = await CaptureVersions(address, secret, state);
            present.AddRange(captured.Present);
            fingerprints.UnionWith(captured.Fingerprints);
        }

        foreach (var secret in await legacy.ListDeletedSecrets(address.Locations))
        {
            highest = Math.Max(highest, secret.Revision.Value);
            if (secret.Revision.Value > state.Erasure.ErasedThrough.Value) throw new EncryptionKeyFenceUnavailable(address.Identifier);
        }

        return (present, highest, fingerprints);
    }

    async Task<(List<(EncryptionKeyRevision Revision, EncryptionKey Key)> Present, HashSet<string> Fingerprints)> CaptureVersions(
        Address address, LegacyAzureKeyVaultEncryptionKeyStorage.ListedSecret secret, EncryptionKeyFenceState state)
    {
        var present = new List<(EncryptionKeyRevision, EncryptionKey)>();
        var fingerprints = new HashSet<string>(StringComparer.Ordinal);
        await foreach (var version in client.GetPropertiesOfSecretVersionsAsync(secret.Name))
        {
            if (version.Tags.TryGetValue(FingerprintTag, out var fingerprint)) fingerprints.Add(fingerprint);
            if (await CaptureMaterial(address, secret, version, state) is { } key) present.Add((secret.Revision, key));
        }

        return (present, fingerprints);
    }

    async Task<EncryptionKey?> CaptureMaterial(Address address, LegacyAzureKeyVaultEncryptionKeyStorage.ListedSecret secret, Azure.Security.KeyVault.Secrets.SecretProperties version, EncryptionKeyFenceState state)
    {
        if (version.Enabled != false)
        {
            var value = await TryRead(secret.Name, version.Version);
            return value is null ? null : LegacyAzureKeyVaultEncryptionKeyStorage.Deserialize(value.Value);
        }

        // New staged versions retain their fingerprint even when disabled. Unknown disabled legacy
        // material cannot be declared erased: silently losing its fingerprint would permit resurrection.
        if (version.Tags.ContainsKey(FingerprintTag) || secret.Revision.Value <= state.Erasure.ErasedThrough.Value) return null;
        throw new EncryptionKeyFenceUnavailable(address.Identifier);
    }

    async Task Sweep(Address address, EncryptionKeyErasureRequest request, EncryptionKeyRevision? onlyRevision = null)
    {
        var names = (await legacy.ListSecrets(address.Locations)).Concat(await legacy.ListDeletedSecrets(address.Locations))
            .Where(_ => _.Revision.Value <= request.Through!.Value && (IsLatest(onlyRevision) || _.Revision == onlyRevision))
            .Select(_ => _.Name).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var name in names) await legacy.DeleteAndPurgeSecret(name);
        if (IsLatest(onlyRevision)) await VerifySweep(address, request);
    }

    async Task VerifySweep(Address address, EncryptionKeyErasureRequest request)
    {
        foreach (var secret in (await legacy.ListSecrets(address.Locations)).Concat(await legacy.ListDeletedSecrets(address.Locations)))
        {
            if (secret.Revision.Value <= request.Through!.Value) throw new EncryptionKeyFenceUnavailable(address.Identifier);
            await VerifyVersions(address, secret.Name, request);
        }
    }

    async Task VerifyVersions(Address address, string name, EncryptionKeyErasureRequest request)
    {
        await foreach (var version in client.GetPropertiesOfSecretVersionsAsync(name))
        {
            // Never purge a name above the captured floor: a successor may share it. Fail closed instead.
            if (await VersionIsCovered(name, version, request)) throw new EncryptionKeyFenceUnavailable(address.Identifier);
        }
    }

    async Task<bool> VersionIsCovered(string name, Azure.Security.KeyVault.Secrets.SecretProperties version, EncryptionKeyErasureRequest request)
    {
        if (version.Tags.TryGetValue(StagedLifecycleTag, out var raw) &&
            uint.TryParse(raw, CultureInfo.InvariantCulture, out var lifecycle) && lifecycle <= request.Lifecycle)
        {
            return true;
        }
        var value = await TryRead(name, version.Version);
        var key = value is null ? null : LegacyAzureKeyVaultEncryptionKeyStorage.Deserialize(value.Value);
        return key is not null && (request.Fingerprints ?? []).Contains(key.Fingerprint, StringComparer.Ordinal);
    }
}
