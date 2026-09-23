// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// The exception that is thrown when an erasure or a new-key authorization is addressed at an
/// <see cref="EncryptionKeyIdentifier"/> that belongs to a plain-confidentiality <c language="csharp">[Encrypted]</c> value
/// rather than to GDPR compliance.
/// </summary>
/// <remarks>
/// A <c language="csharp">[PII]</c> key is erasable because destroying it is the point - a lawful right-to-erasure request.
/// An <c language="csharp">[Encrypted]</c> key protects a secret with no data subject and no lawful basis for erasure; deleting
/// it is unrecoverable data loss, not a compliance act, and no API exposes a way to do it on purpose. This is the
/// defense-in-depth refusal for the one entry point that accepts an arbitrary caller-supplied identifier rather
/// than deriving one from a subject itself - see <see cref="EncryptedValueKeyIdentifiers"/> for why the two
/// identifier spaces cannot collide in the first place.
/// </remarks>
/// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> the erasure or authorization was addressed at.</param>
public class EncryptionKeyIsNotErasable(EncryptionKeyIdentifier identifier)
    : Exception($"The encryption key for identifier '{identifier}' protects a plain-confidentiality [Encrypted] value, not personal data. It has no data subject and no lawful basis for erasure, so it cannot be deleted or have a new lifecycle authorized through PII erasure.");
