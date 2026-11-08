// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>Checks authoritative fences with a bounded, operation-local revocation lease.</summary>
public static class ReleaseKeys
{
    static readonly AsyncLocal<Memo?> _current = new();
    static TimeSpan _grace = TimeSpan.FromSeconds(5);

    /// <summary>Configure the kernel's release grace before it starts serving requests.</summary>
    /// <param name="grace">The erasure confirmation grace.</param>
    public static void Configure(TimeSpan grace)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(grace, TimeSpan.Zero);
        _grace = grace;
    }

    /// <summary>Begin a document, batch, or watch update; nested operations share its reads.</summary>
    /// <param name="timeProvider">Optional clock for this operation.</param>
    /// <param name="revocationGrace">Optional grace for this operation.</param>
    /// <returns>A scope that forgets fences on completion.</returns>
    public static IDisposable BeginScope(TimeProvider? timeProvider = null, TimeSpan? revocationGrace = null)
    {
        var previous = _current.Value;
        _current.Value ??= new Memo(timeProvider ?? TimeProvider.System, revocationGrace ?? _grace);
        return new Scope(previous);
    }

    /// <summary>Get a key only if its revision, material and staged lifecycle are not covered.</summary>
    /// <param name="storage">The key store.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The usable key, or null when erased.</returns>
    public static async Task<EncryptionKey?> TryGetForRelease(IEncryptionKeyStorage storage, EventStoreName eventStore, EventStoreNamespaceName ns, EncryptionKeyIdentifier identifier)
    {
        var pair = await storage.TryGetLatestFor(eventStore, ns, identifier);
        var memo = _current.Value ?? new Memo(TimeProvider.System, _grace);
        var key = new FenceKey(storage, eventStore, ns, identifier);
        while (true)
        {
            var snapshot = await memo.Read(key);
            if (!memo.IsFresh(snapshot)) continue;
            if (pair is null || snapshot.Fence.Covers(pair)) return null;
            memo.Uses[(key, pair)] = true;
            return pair.Key;
        }
    }

    /// <summary>Get a key, reporting an unavailable fence as an empty protected value.</summary>
    /// <param name="storage">The key store.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The usable key, or null when refused.</returns>
    public static async Task<EncryptionKey?> TryGetForReleaseOrEmpty(IEncryptionKeyStorage storage, EventStoreName eventStore, EventStoreNamespaceName ns, EncryptionKeyIdentifier identifier)
    {
        try
        {
            return await TryGetForRelease(storage, eventStore, ns, identifier);
        }
        catch (EncryptionKeyFenceUnavailable)
        {
            Trace.TraceWarning("Encryption key fence unavailable; releasing an empty protected value.");
            return null;
        }
    }

    /// <summary>Read a bounded, single-flight fence within the current operation.</summary>
    /// <param name="storage">The key store.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="identifier">The identifier.</param>
    /// <returns>The authoritative erasure fence for this read.</returns>
    public static async Task<EncryptionKeyErasure?> GetErasureForRelease(IEncryptionKeyStorage storage, EventStoreName eventStore, EventStoreNamespaceName ns, EncryptionKeyIdentifier identifier)
    {
        var fence = (await (_current.Value ?? new Memo(TimeProvider.System, _grace)).Read(new(storage, eventStore, ns, identifier))).Fence;

        // A protocol authority initializes an empty fence for provisioning; that is not evidence of erasure.
        return fence is { NewKeyAllowed: true, Lifecycle: 1, ErasedLifecycle: 0 } &&
            fence.ErasedThrough.Value == 0 && fence.ErasedKeyFingerprints.Count == 0 ? null : fence;
    }

    /// <summary>Revalidate all material immediately before the synchronous hand-off.</summary>
    /// <typeparam name="T">The released result.</typeparam>
    /// <param name="handoff">The synchronous hand-off, with no intervening await.</param>
    /// <returns>The value produced by the hand-off.</returns>
    /// <exception cref="EncryptionKeyErased">Material was erased during this operation.</exception>
    public static async Task<T> Complete<T>(Func<T> handoff)
    {
        var memo = _current.Value;
        if (memo is null) return handoff();
        while (true)
        {
            List<Snapshot> checkedReads = [];
            foreach (var (use, _) in memo.Uses)
            {
                var snapshot = await memo.Read(use.Key);
                if (snapshot.Fence.Covers(use.Pair)) throw new EncryptionKeyErased(use.Key.Identifier, snapshot.Fence!);
                checkedReads.Add(snapshot);
            }

            // Refresh can itself stall. All reads must still be inside the lease at the actual hand-off.
            if (checkedReads.TrueForAll(memo.IsFresh)) return handoff();
        }
    }

    /// <summary>Track provisioned material until the append hand-off.</summary>
    /// <param name="storage">The key store.</param>
    /// <param name="address">The event store, namespace and identifier of the key.</param>
    /// <param name="pair">The provisioned revision.</param>
    public static void Track(IEncryptionKeyStorage storage, (EventStoreName EventStore, EventStoreNamespaceName Namespace, EncryptionKeyIdentifier Identifier) address, EncryptionKeyAtRevision pair)
    {
        if (_current.Value is { } memo) memo.Uses[(new(storage, address.EventStore, address.Namespace, address.Identifier), pair)] = true;
    }

    sealed record FenceKey(IEncryptionKeyStorage Storage, EventStoreName EventStore, EventStoreNamespaceName Namespace, EncryptionKeyIdentifier Identifier);
    sealed record Snapshot(EncryptionKeyErasure? Fence, ulong Version, long Started, DateTimeOffset WallStarted);

    sealed class Memo(TimeProvider clock, TimeSpan grace)
    {
        readonly ConcurrentDictionary<FenceKey, Lazy<Task<Snapshot>>> _reads = new(new FenceKeyComparer());
        readonly TimeSpan _lease = grace > TimeSpan.Zero ? grace / 2 : throw new ArgumentOutOfRangeException(nameof(grace));
        Exception? _failure;

        public ConcurrentDictionary<(FenceKey Key, EncryptionKeyAtRevision Pair), bool> Uses { get; } = new();

        public bool IsFresh(Snapshot snapshot) =>
            clock.GetElapsedTime(snapshot.Started) <= _lease && clock.GetUtcNow() - snapshot.WallStarted <= _lease;

        public async Task<Snapshot> Read(FenceKey key)
        {
            while (true)
            {
                if (_failure is { } failure) throw new EncryptionKeyFenceUnavailable(failure);
                var pending = _reads.GetOrAdd(key, static (scope, memo) => new(() => memo.ReadFresh(scope)), this);
                Snapshot snapshot;
                try
                {
                    snapshot = await pending.Value;
                }
                catch (Exception error)
                {
                    _failure = error;
                    _reads.Clear();
                    throw;
                }

                if (IsFresh(snapshot)) return snapshot;
                _reads.TryRemove(new KeyValuePair<FenceKey, Lazy<Task<Snapshot>>>(key, pending));
            }
        }

        async Task<Snapshot> ReadFresh(FenceKey key)
        {
            var started = clock.GetTimestamp();
            var wallStarted = clock.GetUtcNow();
            try
            {
                var fence = await key.Storage.GetErasureForRelease(key.EventStore, key.Namespace, key.Identifier);
                return new(fence, fence?.Version ?? 0, started, wallStarted);
            }
            catch (Exception error) when (error is not EncryptionKeyFenceUnavailable)
            {
                throw new EncryptionKeyFenceUnavailable(error);
            }
        }
    }

    sealed class FenceKeyComparer : IEqualityComparer<FenceKey>
    {
        public bool Equals(FenceKey? x, FenceKey? y) => x is not null && y is not null && ReferenceEquals(x.Storage, y.Storage) &&
            x.EventStore == y.EventStore && x.Namespace == y.Namespace && x.Identifier == y.Identifier;

        public int GetHashCode(FenceKey obj) => HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Storage), obj.EventStore, obj.Namespace, obj.Identifier);
    }

    sealed class Scope(Memo? previous) : IDisposable
    {
        public void Dispose() => _current.Value = previous;
    }
}
