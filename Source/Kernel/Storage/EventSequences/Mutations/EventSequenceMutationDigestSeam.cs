// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// The single place that turns canonical mutation bytes into a digest.
/// </summary>
/// <remarks>
/// Every definition and receipt digest goes through <see cref="Compute"/>, which takes the event store, the
/// <see cref="EventSequenceMutationDigestVersion"/> to compute with and the canonical bytes. The version is the
/// persisted one for anything already stored, so a stored digest is always recomputed with the algorithm that produced
/// it. <see cref="CurrentVersion"/> is only the version new digests are written with; <see cref="IsSupported"/>
/// is the set of versions that can still be read.
/// <para>
/// <see cref="EventSequenceMutationDigestVersion.HmacSha256PerEventStore"/> is keyed. The key is not looked up here:
/// the registry resolves it asynchronously before it touches any digest and puts it in scope with <see cref="UseKey"/>
/// for the rest of the operation. Computing a keyed digest without the event store's key in scope throws
/// <see cref="EventSequenceMutationDigestKeyUnavailable"/>; it never falls back to an unkeyed version.
/// </para>
/// </remarks>
static class EventSequenceMutationDigestSeam
{
    static readonly ImmutableDictionary<EventSequenceMutationDigestVersion, Func<EventStoreName, byte[], byte[]>> _productionVersions =
        ImmutableDictionary<EventSequenceMutationDigestVersion, Func<EventStoreName, byte[], byte[]>>.Empty
            .Add(EventSequenceMutationDigestVersion.Sha256DomainSeparated, static (_, bytes) => SHA256.HashData(bytes))
            .Add(EventSequenceMutationDigestVersion.HmacSha256PerEventStore, static (eventStore, bytes) => KeyFor(eventStore).Compute(bytes));

    static readonly Registry _production = new(EventSequenceMutationDigestVersion.HmacSha256PerEventStore, _productionVersions);
    static readonly AsyncLocal<Registry?> _override = new();
    static readonly AsyncLocal<KeyScope?> _key = new();
    static Func<EventStoreName, EventSequenceMutationDigestKey>? _testKeys;

    /// <summary>
    /// Gets the version that new digests are written with.
    /// </summary>
    internal static EventSequenceMutationDigestVersion CurrentVersion => GetActive().Write;

    /// <summary>
    /// Determines whether digests of a version can be computed, and therefore read.
    /// </summary>
    /// <param name="version">The version to check.</param>
    /// <returns><see langword="true"/> when the version is supported.</returns>
    internal static bool IsSupported(EventSequenceMutationDigestVersion version) => GetActive().Versions.ContainsKey(version);

    /// <summary>
    /// Computes the digest of canonical bytes for an event store with a specific version.
    /// </summary>
    /// <param name="eventStore">The event store the digest belongs to.</param>
    /// <param name="version">The version to compute with.</param>
    /// <param name="canonicalBytes">The canonical, domain-separated bytes.</param>
    /// <returns>The 32 digest bytes.</returns>
    /// <exception cref="UnsupportedEventSequenceMutationDigestVersion">Thrown when the version is not supported. Callers check <see cref="IsSupported"/> first and fail closed.</exception>
    internal static byte[] Compute(EventStoreName eventStore, EventSequenceMutationDigestVersion version, ReadOnlySpan<byte> canonicalBytes) =>
        GetActive().Versions.TryGetValue(version, out var compute)
            ? compute(eventStore, canonicalBytes.ToArray())
            : throw new UnsupportedEventSequenceMutationDigestVersion(version);

    /// <summary>
    /// Registers an additional version, optionally making it the write version, until the returned scope is disposed.
    /// </summary>
    /// <param name="version">The version to register.</param>
    /// <param name="compute">The digest function of the version.</param>
    /// <param name="makeCurrent">Whether new digests are written with the version.</param>
    /// <returns>The scope that restores the previous registration on disposal.</returns>
    /// <remarks>
    /// This is a test seam. The registration is scoped to the current asynchronous flow, so specs running in parallel
    /// never see each other's versions.
    /// </remarks>
    internal static IDisposable RegisterForTest(EventSequenceMutationDigestVersion version, Func<EventStoreName, byte[], byte[]> compute, bool makeCurrent)
    {
        var previous = _override.Value;
        var active = GetActive();
        _override.Value = new(makeCurrent ? version : active.Write, active.Versions.SetItem(version, compute));
        return new Restore(previous);
    }

    /// <summary>
    /// Changes the write version among the registered versions until the returned scope is disposed. A test seam.
    /// </summary>
    /// <param name="version">The version new digests are written with.</param>
    /// <returns>The scope that restores the previous write version on disposal.</returns>
    internal static IDisposable SetCurrentForTest(EventSequenceMutationDigestVersion version)
    {
        var previous = _override.Value;
        _override.Value = new(version, GetActive().Versions);
        return new Restore(previous);
    }

    /// <summary>
    /// Stops supporting a version until the returned scope is disposed. A test seam.
    /// </summary>
    /// <param name="version">The version that can no longer be read or written.</param>
    /// <returns>The scope that restores the previous registration on disposal.</returns>
    internal static IDisposable WithoutVersionForTest(EventSequenceMutationDigestVersion version)
    {
        var previous = _override.Value;
        var active = GetActive();
        _override.Value = new(active.Write, active.Versions.Remove(version));
        return new Restore(previous);
    }

    /// <summary>
    /// Puts the key of an event store in scope for keyed digests until the returned scope is disposed.
    /// </summary>
    /// <param name="key">The key, or <see langword="null"/> to compute without any key.</param>
    /// <returns>The scope that restores the previous key on disposal.</returns>
    /// <remarks>
    /// The key is scoped to the current asynchronous flow, so concurrent operations on different event stores never
    /// see each other's key.
    /// </remarks>
    internal static IDisposable UseKey(EventSequenceMutationDigestKey? key)
    {
        var previous = _key.Value;
        _key.Value = new(key);
        return new RestoreKey(previous);
    }

    /// <summary>
    /// Provides keys for every flow that has no key in scope. A test seam: production never calls it.
    /// </summary>
    /// <param name="keys">The keys per event store.</param>
    /// <remarks>
    /// Spec projects call this from a module initializer, so the many specs that compute digests directly do not each
    /// have to put a key in scope. A flow with an explicit <see cref="UseKey"/> scope, including one with no key,
    /// never sees these keys.
    /// </remarks>
    internal static void UseKeysForAllFlowsForTest(Func<EventStoreName, EventSequenceMutationDigestKey> keys) => _testKeys = keys;

    static Registry GetActive() => _override.Value ?? _production;

    static EventSequenceMutationDigestKey KeyFor(EventStoreName eventStore)
    {
        var key = _key.Value is { } scope ? scope.Key : _testKeys?.Invoke(eventStore);
        return key is not null && key.EventStore == eventStore
            ? key
            : throw new EventSequenceMutationDigestKeyUnavailable(eventStore);
    }

    sealed record Registry(EventSequenceMutationDigestVersion Write, ImmutableDictionary<EventSequenceMutationDigestVersion, Func<EventStoreName, byte[], byte[]>> Versions);

    sealed class Restore(Registry? previous) : IDisposable
    {
        public void Dispose() => _override.Value = previous;
    }

    sealed record KeyScope(EventSequenceMutationDigestKey? Key);

    sealed class RestoreKey(KeyScope? previous) : IDisposable
    {
        public void Dispose() => _key.Value = previous;
    }
}
