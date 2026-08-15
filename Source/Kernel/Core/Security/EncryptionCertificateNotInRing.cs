// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Exception that gets thrown when a protected value names a certificate the ring does not hold.
/// </summary>
/// <param name="keyId">The key id the value needs.</param>
/// <param name="availableKeyIds">The key ids the ring holds.</param>
/// <remarks>
/// The value was protected by a certificate that has been retired from the ring. The message names the key
/// id required and the key ids loaded, and nothing else — never the protected value itself.
/// </remarks>
public class EncryptionCertificateNotInRing(string keyId, IEnumerable<string> availableKeyIds)
    : Exception(
        $"The value was protected by the encryption certificate with key id '{keyId}', which is not in the ring. " +
        $"The ring holds: {Describe(availableKeyIds)}. Put the certificate back under " +
        "'EncryptionCertificate:Previous' to read the value; it cannot be recovered without it.")
{
    static string Describe(IEnumerable<string> keyIds) =>
        keyIds.Any() ? string.Join(", ", keyIds) : "no certificates";
}
