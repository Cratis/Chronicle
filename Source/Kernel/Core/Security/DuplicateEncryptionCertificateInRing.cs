// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Exception that gets thrown when the same certificate appears more than once in the encryption-certificate ring.
/// </summary>
/// <param name="keyId">The key id that appears more than once.</param>
public class DuplicateEncryptionCertificateInRing(string keyId)
    : Exception(
        $"The certificate with key id '{keyId}' appears more than once in the encryption-certificate ring. " +
        "A ring where the active and a previous position hold the same key pair looks like a rotation but " +
        "provides no overlap at all, so it is rejected rather than accepted as one.");
