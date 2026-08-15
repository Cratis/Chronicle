// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Exception that gets thrown when a value carrying no key id cannot be decrypted by any certificate in the ring.
/// </summary>
/// <param name="availableKeyIds">The key ids the ring holds.</param>
/// <remarks>
/// Values written before Chronicle started labeling ciphertext with the key id carry no key id, so the only
/// way to read one is to try every certificate in the ring. Reaching the end means the certificate that
/// protected it has been retired.
/// </remarks>
public class ValueNotDecryptableWithAnyCertificate(IEnumerable<string> availableKeyIds)
    : Exception(
        "The value carries no key id and none of the certificates in the encryption-certificate ring decrypt it. " +
        $"The ring holds: {Describe(availableKeyIds)}. It was protected by a certificate that is no longer in the ring.")
{
    static string Describe(IEnumerable<string> keyIds) =>
        keyIds.Any() ? string.Join(", ", keyIds) : "no certificates";
}
