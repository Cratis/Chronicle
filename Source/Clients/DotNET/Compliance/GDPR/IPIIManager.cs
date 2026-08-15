// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Defines a manager of PII in the system.
/// </summary>
public interface IPIIManager
{
    /// <summary>
    /// Erase the encryption key for a subject, making every PII value protected under it permanently unreadable.
    /// </summary>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to erase.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The erasure covers <b>every event store in this manager's namespace</b>, not only the event store it was
    /// resolved from. Chronicle copies a subject's key into any event store it forwards their events into, so that
    /// set is exactly where the key can have reached; erasing less than all of it leaves a copy that is put back
    /// the next time an event is forwarded. It also records the erasure, so nothing provisions or copies a key for
    /// the subject afterwards - appending a PII value for them fails rather than silently minting a new key, until
    /// <see cref="AllowNewEncryptionKeyFor"/> authorizes one.
    /// </remarks>
    Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier);

    /// <summary>
    /// Authorize a new encryption key for a subject that was erased, so a later lawful lifecycle can protect their
    /// data again.
    /// </summary>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to authorize a new key for.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Erasing a subject removes the key that exists now; it does not ban the identifier forever. This creates no
    /// key - it lets the next PII value written for the subject provision a fresh, independent one, which can
    /// decrypt nothing written before the erasure. The erased key itself never comes back, whatever else happens.
    /// </remarks>
    Task AllowNewEncryptionKeyFor(EncryptionKeyIdentifier identifier);
}
