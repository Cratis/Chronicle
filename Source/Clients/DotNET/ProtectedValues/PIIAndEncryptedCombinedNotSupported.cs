// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// The exception that is thrown when a property, or the type it resolves compliance metadata from, carries both
/// <see cref="Compliance.GDPR.PIIAttribute"/> and <see cref="EncryptedAttribute"/>.
/// </summary>
/// <remarks>
/// This is not merely redundant - it corrupts the value. <c language="csharp">JsonComplianceManager</c> applies every
/// matching handler for a property in sequence, so a value marked both ways is encrypted first under the PII key
/// and then again under the Encrypted key; releasing it decrypts with the wrong key against ciphertext, which
/// fails loudly (a padding/authentication error) rather than returning a wrong value. A value needs exactly one
/// protection: PII when it is personal data with a lawful basis for erasure, Encrypted when it is an operational
/// secret with none. See the remarks on <see cref="EncryptedAttribute"/> for how to choose.
/// </remarks>
/// <param name="property">The name of the property carrying both attributes.</param>
public class PIIAndEncryptedCombinedNotSupported(string property)
    : Exception($"'{property}' carries both [PII] and [Encrypted]. A value needs exactly one protection - combining them would encrypt it twice, under two different keys, and it cannot be released correctly. Choose [PII] for personal data with a lawful basis for erasure, or [Encrypted] for an operational secret with none.");
