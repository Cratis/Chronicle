// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Thrown when the signed identity of a v3 protected value cannot be verified at the address it is released from.
/// </summary>
/// <remarks>
/// Strict release fails closed on this: the value is neither provably erased nor provably intact.
/// </remarks>
public class EncryptionEnvelopeBindingFailed : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionEnvelopeBindingFailed"/> class.
    /// </summary>
    /// <param name="reason">What failed to verify. It never carries the address or any key material.</param>
    public EncryptionEnvelopeBindingFailed(string reason)
        : base($"The managed-key identity of the protected value could not be verified: {reason}.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionEnvelopeBindingFailed"/> class.
    /// </summary>
    /// <param name="reason">What failed to verify.</param>
    /// <param name="innerException">The cryptographic failure.</param>
    public EncryptionEnvelopeBindingFailed(string reason, Exception innerException)
        : base($"The managed-key identity of the protected value could not be verified: {reason}.", innerException)
    {
    }
}
