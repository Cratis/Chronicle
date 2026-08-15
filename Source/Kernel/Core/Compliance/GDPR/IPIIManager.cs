// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents a manager of PII in the system.
/// </summary>
public interface IPIIManager : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Erase the encryption key for an <see cref="EncryptionKeyIdentifier"/>, making every PII value protected
    /// under it permanently unreadable.
    /// </summary>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to erase.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="Storage.Compliance.EncryptionKeyErasureIncomplete">Thrown when the erasure did not reach every event store.</exception>
    /// <remarks>
    /// The erasure covers <b>every event store in this manager's namespace</b>, not only the one it was addressed
    /// at. A cross-event-store subscription copies a subject's key into the event store it forwards into, and never
    /// across namespaces - so that set is exactly where the key can have reached, and erasing less than all of it
    /// leaves a copy that is healed back the next time an event is forwarded.
    /// </remarks>
    Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier);

    /// <summary>
    /// Authorize a later lawful lifecycle for an erased <see cref="EncryptionKeyIdentifier"/>, so that the next
    /// PII value written for the subject provisions a fresh key.
    /// </summary>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to authorize a new key for.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="Storage.Compliance.EncryptionKeyLifecycleIncomplete">Thrown when the authorization did not reach every event store.</exception>
    /// <remarks>
    /// Erasure removes the key incarnation that exists now; it is not a permanent ban on the subject identifier.
    /// This creates no key itself and never brings the erased one back - it lifts the refusal so that the next
    /// append mints an independent successor, which can decrypt nothing written before the erasure. Like the
    /// erasure it covers every event store in the namespace, because a subject fenced in one of them and open in
    /// another cannot have their events forwarded between the two.
    /// </remarks>
    Task AllowNewEncryptionKeyFor(EncryptionKeyIdentifier identifier);
}
