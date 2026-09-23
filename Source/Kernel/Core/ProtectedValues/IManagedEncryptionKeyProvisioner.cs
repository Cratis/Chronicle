// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Defines a system that provisions a managed <see cref="EncryptionKey"/> for an <see cref="EncryptionKeyIdentifier"/>,
/// minting one the first time it is needed and reusing it for every caller afterwards.
/// </summary>
/// <remarks>
/// This is the shared "get-or-create, exactly once" primitive behind every value-protection feature built on
/// <see cref="IEncryptionKeyStorage"/> - GDPR compliance (<see cref="Compliance.GDPR.PIICompliancePropertyValueHandler"/>)
/// and plain-confidentiality encryption (<see cref="EncryptedValueHandler"/>) alike. It is deliberately neutral:
/// it knows nothing about subjects, erasure, or any other policy a particular feature layers on top - it only
/// guarantees that a given <see cref="EncryptionKeyIdentifier"/> converges on one persisted key no matter how many
/// callers ask for it at once. What makes it worth sharing rather than re-deriving per feature is that
/// concurrency contract, not the two lines that call <see cref="IEncryptionKeyStorage.GetOrAddFor"/>: a batch
/// append and its sibling projections provision the same identifier concurrently within one process
/// (<c language="csharp">Task.WhenAll</c>), and the same identifier can be provisioned from more than one silo at
/// once. <see cref="IEncryptionKeyStorage.GetOrAddFor"/> is the cross-process convergence primitive; the
/// in-process gate this sits in front of it for is what stops every one of those concurrent callers minting and
/// discarding a throwaway key before the store's own convergence has a chance to run.
/// </remarks>
public interface IManagedEncryptionKeyProvisioner
{
    /// <summary>
    /// Ensure a managed <see cref="EncryptionKey"/> exists for an <see cref="EncryptionKeyIdentifier"/>, provisioning
    /// one if none exists yet.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the key belongs to.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the key belongs to.</param>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to provision for.</param>
    /// <returns>The existing or newly provisioned <see cref="EncryptionKey"/>.</returns>
    Task<EncryptionKey> EnsureKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier);
}
