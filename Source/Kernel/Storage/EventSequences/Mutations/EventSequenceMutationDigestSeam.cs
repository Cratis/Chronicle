// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// The single place that turns canonical mutation bytes into a digest.
/// </summary>
/// <remarks>
/// Every definition and receipt digest goes through <see cref="Compute"/>, which takes the event store and the
/// canonical bytes and returns the digest together with the <see cref="EventSequenceMutationDigestVersion"/> that
/// produced it. A keyed construction (for example HMAC-SHA256 with a per-event-store key) replaces this one method
/// and bumps <see cref="CurrentVersion"/> without touching any caller. No key management exists here.
/// </remarks>
static class EventSequenceMutationDigestSeam
{
    /// <summary>
    /// Gets the version that <see cref="Compute"/> currently produces.
    /// </summary>
    internal static EventSequenceMutationDigestVersion CurrentVersion => EventSequenceMutationDigestVersion.Sha256DomainSeparated;

    /// <summary>
    /// Computes the digest of canonical bytes for an event store.
    /// </summary>
    /// <param name="eventStore">The event store the digest belongs to.</param>
    /// <param name="canonicalBytes">The canonical, domain-separated bytes.</param>
    /// <returns>The version that produced the digest and the 32 digest bytes.</returns>
    internal static (EventSequenceMutationDigestVersion Version, byte[] Digest) Compute(EventStoreName eventStore, ReadOnlySpan<byte> canonicalBytes)
    {
        _ = eventStore;
        return (EventSequenceMutationDigestVersion.Sha256DomainSeparated, SHA256.HashData(canonicalBytes));
    }
}
