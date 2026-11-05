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
/// is the set of versions that can still be read. A keyed construction (for example HMAC-SHA256 with a per-event-store
/// key) is added as a new version and made current without touching any caller. No key management exists here.
/// </remarks>
static class EventSequenceMutationDigestSeam
{
    static readonly ImmutableDictionary<EventSequenceMutationDigestVersion, Func<EventStoreName, byte[], byte[]>> _productionVersions =
        ImmutableDictionary<EventSequenceMutationDigestVersion, Func<EventStoreName, byte[], byte[]>>.Empty
            .Add(EventSequenceMutationDigestVersion.Sha256DomainSeparated, static (_, bytes) => SHA256.HashData(bytes));

    static readonly Registry _production = new(EventSequenceMutationDigestVersion.Sha256DomainSeparated, _productionVersions);
    static readonly AsyncLocal<Registry?> _override = new();

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

    static Registry GetActive() => _override.Value ?? _production;

    sealed record Registry(EventSequenceMutationDigestVersion Write, ImmutableDictionary<EventSequenceMutationDigestVersion, Func<EventStoreName, byte[], byte[]>> Versions);

    sealed class Restore(Registry? previous) : IDisposable
    {
        public void Dispose() => _override.Value = previous;
    }
}
