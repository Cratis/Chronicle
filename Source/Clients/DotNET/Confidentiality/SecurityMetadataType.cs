// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Confidentiality;

/// <summary>
/// Represents a type of security metadata.
/// </summary>
/// <param name="Value">Underlying value.</param>
/// <remarks>
/// This is the security counterpart to <see cref="Compliance.ComplianceMetadataType"/> - a deliberately separate
/// type, not a shared one, because security and compliance answer different questions about a value and are
/// governed by different rules. See <see cref="Schemas.SchemaMetadataCategory"/> for why the two are never allowed
/// to mix.
/// </remarks>
public record SecurityMetadataType(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// A value that is subject-scoped, plain-confidentiality encryption - a security measure, not a compliance one.
    /// Marked with <c language="csharp">[Encrypted]</c> on the client rather than <c language="csharp">[PII]</c>; the encryption
    /// key it is provisioned under is never erasable and is never the same key a <c language="csharp">[PII]</c> value for the
    /// same subject uses.
    /// </summary>
    public static readonly SecurityMetadataType EncryptedSubject = new("EncryptedSubject");

    /// <summary>
    /// A value that is namespace-scoped, plain-confidentiality encryption. One key protects every value marked
    /// this way in a given namespace, regardless of which document it came from. Marked with
    /// <c language="csharp">[Encrypted(EncryptionScope.Namespace)]</c> on the client; never erasable, and never the same
    /// key a <c language="csharp">[PII]</c> value uses - <c language="csharp">[PII]</c> is always subject-scoped and never
    /// gains this scope.
    /// </summary>
    public static readonly SecurityMetadataType EncryptedNamespace = new("EncryptedNamespace");

    /// <summary>
    /// A value that is globally-scoped, plain-confidentiality encryption. One key protects every value marked this
    /// way across the whole installation - every event store, every namespace. Marked with
    /// <c language="csharp">[Encrypted(EncryptionScope.Global)]</c> on the client; never erasable, and never the same key
    /// a <c language="csharp">[PII]</c> value uses - <c language="csharp">[PII]</c> is always subject-scoped and never gains
    /// this scope.
    /// </summary>
    public static readonly SecurityMetadataType EncryptedGlobal = new("EncryptedGlobal");

    /// <summary>
    /// Convert from a <see cref="string"/> to <see cref="SecurityMetadataType"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    public static implicit operator SecurityMetadataType(string value) => new(value);

    /// <summary>
    /// Convert from <see cref="SecurityMetadataType"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="SecurityMetadataType"/> to convert from.</param>
    public static implicit operator string(SecurityMetadataType value) => value.Value;
}
