// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the position a certificate holds in the encryption-certificate ring.
/// </summary>
public enum EncryptionCertificateRole
{
    /// <summary>
    /// No position. Nothing in the ring and nothing in a rotation report ever carries this.
    /// </summary>
    /// <remarks>
    /// It exists so an unset role reads as obviously unset rather than silently as <see cref="Active"/>.
    /// </remarks>
    None = 0,

    /// <summary>
    /// The certificate everything written from now on is protected with. There is exactly one.
    /// </summary>
    Active = 1,

    /// <summary>
    /// A certificate that was active before and is kept loaded so what it protects stays readable.
    /// </summary>
    Previous = 2,

    /// <summary>
    /// A certificate that stored data still depends on but that is no longer in the ring.
    /// </summary>
    /// <remarks>
    /// Nothing in the ring ever carries this role. It appears in the rotation diagnostic when stored data is
    /// found to be protected by a certificate that is neither active nor previous — that data is already
    /// unreadable, and it is what a rotation carried out in the wrong order looks like.
    /// </remarks>
    Retired = 3
}
