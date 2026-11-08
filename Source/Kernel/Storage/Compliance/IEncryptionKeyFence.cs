// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Database authority for a key store whose key-material service cannot perform conditional writes.
/// </summary>
/// <remarks>
/// State is isolated from the database's own key store. Reads include insert-only initialization and must be
/// linearizable; writes are acknowledged CAS operations. A write that timed out, was cancelled or lost its response throws
/// <see cref="EncryptionKeyFenceWriteOutcomeUnknown"/>; any other failure throws <see cref="EncryptionKeyFenceUnavailable"/>.
/// No material is stored here. Callers retain the originally observed lifecycle across retries.
/// </remarks>
public interface IEncryptionKeyFence
{
    /// <summary>Gets whether the authority survives process loss.</summary>
    bool IsDurable { get; }

    /// <summary>Read or initialize an external store's authoritative fence.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="identifier">The external store's scoped identifier.</param>
    /// <returns>The current state.</returns>
    Task<EncryptionKeyFenceState> ReadFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);

    /// <summary>Replace exactly the observed state, advancing its version once.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="identifier">The external store's scoped identifier.</param>
    /// <param name="observed">The observed state.</param>
    /// <param name="next">The monotonic replacement.</param>
    /// <returns>False only for a definite CAS mismatch.</returns>
    Task<bool> CompareExchangeFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyFenceState observed, EncryptionKeyFenceState next);

    /// <summary>Publish an exact external version only while its validated fence remains current.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="eventStoreNamespace">The namespace.</param>
    /// <param name="identifier">The external store's scoped identifier.</param>
    /// <param name="observed">The validated state.</param>
    /// <param name="revision">The writer's exact version.</param>
    /// <returns>Whether publication won.</returns>
    Task<bool> PublishRevision(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier, EncryptionKeyFenceState observed, EncryptionKeyPublication revision) =>
        CompareExchangeFor(eventStore, eventStoreNamespace, identifier, observed, observed.Publish(identifier, revision));
}
