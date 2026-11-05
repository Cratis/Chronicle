// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Defines where the per-event-store secret behind mutation receipt digests lives in the encryption key store.
/// </summary>
/// <remarks>
/// <para>
/// The secret sits next to subject keys, so the identifier carries a reserved <see cref="Marker"/>. The compliance
/// surface refuses any identifier with the marker: it cannot be erased, its lifecycle cannot be re-authorized, and PII
/// cannot be provisioned under it. Erasing a subject therefore never touches the digest secret, and a caller-supplied
/// subject can never resolve to it.
/// </para>
/// <para>
/// This is a sibling of the encrypted-value key identifiers in the kernel's protected values, and is checked by the
/// same guards.
/// </para>
/// </remarks>
public static class EventSequenceMutationDigestKeyIdentifiers
{
    /// <summary>
    /// The prefix that marks an identifier as reserved for mutation receipt digests.
    /// </summary>
    public const string Marker = "$chronicle-mutation-receipt-digest$";

    /// <summary>
    /// The namespace the secret of an event store is stored under. The secret is per event store, not per namespace.
    /// </summary>
    public static readonly EventStoreNamespaceName Namespace = EventStoreNamespaceName.Default;

    /// <summary>
    /// The identifier the secret of an event store is stored under.
    /// </summary>
    public static readonly EncryptionKeyIdentifier Secret = new($"{Marker}v2$");

    /// <summary>
    /// Determines whether an identifier is reserved for mutation receipt digests.
    /// </summary>
    /// <param name="identifier">The identifier to check.</param>
    /// <returns><see langword="true"/> when the identifier carries the reserved marker.</returns>
    public static bool IsReserved(EncryptionKeyIdentifier identifier) => identifier.Value.StartsWith(Marker, StringComparison.Ordinal);
}
