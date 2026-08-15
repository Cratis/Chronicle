// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Defines the ordered set of encryption certificates a Chronicle node runs with.
/// </summary>
/// <remarks>
/// <para>
/// One certificate is <b>active</b> and protects everything written from now on. Zero or more <b>previous</b>
/// certificates stay loaded so values written while they were active remain readable. That overlap is what
/// makes a rotation possible without downtime.
/// </para>
/// <para>
/// The ring is resolved once and held for the lifetime of the process, so adding, promoting or retiring a
/// certificate takes a restart. A ring that is configured but cannot be resolved fails rather than falling
/// back to a smaller ring — a certificate quietly missing from the ring is data quietly becoming unreadable.
/// </para>
/// </remarks>
public interface IEncryptionCertificateRing
{
    /// <summary>
    /// Gets a value indicating whether an encryption certificate is configured.
    /// </summary>
    /// <remarks>
    /// Answered from configuration alone, so it never throws for a ring that is configured but broken.
    /// </remarks>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets the certificate everything written from now on is protected with.
    /// </summary>
    /// <exception cref="EncryptionCertificateNotConfigured">Thrown when no encryption certificate is configured.</exception>
    EncryptionCertificateRingEntry Active { get; }

    /// <summary>
    /// Gets the certificates kept for decryption only, most recently retired first.
    /// </summary>
    IEnumerable<EncryptionCertificateRingEntry> Previous { get; }

    /// <summary>
    /// Gets every certificate in the ring, the active one first.
    /// </summary>
    /// <remarks>
    /// This is the order to hand to anything that accepts a set of certificates for decryption.
    /// </remarks>
    IEnumerable<EncryptionCertificateRingEntry> All { get; }

    /// <summary>
    /// Finds the certificate a protected value names.
    /// </summary>
    /// <param name="keyId">The key id to find.</param>
    /// <returns>The matching <see cref="EncryptionCertificateRingEntry"/>, or <see langword="null"/> when the ring does not hold it.</returns>
    EncryptionCertificateRingEntry? Find(string keyId);

    /// <summary>
    /// Describes the ring without exposing key material.
    /// </summary>
    /// <returns>The current <see cref="EncryptionCertificateRingStatus"/>.</returns>
    EncryptionCertificateRingStatus GetStatus();
}
