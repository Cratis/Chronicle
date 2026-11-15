// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// A fence write was sent but timed out, was cancelled or lost its response, so it may have been applied.
/// </summary>
/// <remarks>
/// Unlike its base, which means nothing happened, this outcome is unknown. Callers still fail closed. A caller may retry only
/// with the same request id, which the store deduplicates; it must never turn the outcome into a fresh erasure.
/// </remarks>
public class EncryptionKeyFenceWriteOutcomeUnknown : EncryptionKeyFenceUnavailable
{
    /// <summary>Initializes an unknown write outcome for an identifier.</summary>
    /// <param name="identifier">The key identifier.</param>
    /// <param name="cause">The timeout, cancellation or lost response.</param>
    public EncryptionKeyFenceWriteOutcomeUnknown(EncryptionKeyIdentifier identifier, Exception cause)
        : base($"A write to the encryption key fence for identifier '{identifier}' may have been applied; its outcome is unknown.", cause)
    {
    }

    /// <summary>Initializes an unknown write outcome for fence metadata not addressed by an identifier.</summary>
    /// <param name="cause">The timeout, cancellation or lost response.</param>
    public EncryptionKeyFenceWriteOutcomeUnknown(Exception cause)
        : base("A write to the encryption key erasure fence may have been applied; its outcome is unknown.", cause)
    {
    }
}
