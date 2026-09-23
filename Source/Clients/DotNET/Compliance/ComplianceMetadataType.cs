// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Represents a type of compliance metadata.
/// </summary>
/// <param name="Value">Underlying value.</param>
public record ComplianceMetadataType(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Personally Identifiable Information according to the definition of Personal Data in GDPR.
    /// </summary>
    public static readonly ComplianceMetadataType PII = new("PII");

    /// <summary>
    /// A value that is subject-scoped, plain-confidentiality encryption - a security measure, not a compliance one.
    /// Marked with <c language="csharp">[Encrypted]</c> on the client rather than <c language="csharp">[PII]</c>; the encryption
    /// key it is provisioned under is never erasable and is never the same key a <c language="csharp">[PII]</c> value for the
    /// same subject uses.
    /// </summary>
    public static readonly ComplianceMetadataType EncryptedSubject = new("EncryptedSubject");

    /// <summary>
    /// A value that is namespace-scoped, plain-confidentiality encryption. One key protects every value marked
    /// this way in a given namespace, regardless of which document it came from. Marked with
    /// <c language="csharp">[Encrypted(EncryptionScope.Namespace)]</c> on the client; never erasable, and never the same
    /// key a <c language="csharp">[PII]</c> value uses - <c language="csharp">[PII]</c> is always subject-scoped and never
    /// gains this scope.
    /// </summary>
    public static readonly ComplianceMetadataType EncryptedNamespace = new("EncryptedNamespace");

    /// <summary>
    /// A value that is globally-scoped, plain-confidentiality encryption. One key protects every value marked this
    /// way across the whole installation - every event store, every namespace. Marked with
    /// <c language="csharp">[Encrypted(EncryptionScope.Global)]</c> on the client; never erasable, and never the same key
    /// a <c language="csharp">[PII]</c> value uses - <c language="csharp">[PII]</c> is always subject-scoped and never gains
    /// this scope.
    /// </summary>
    public static readonly ComplianceMetadataType EncryptedGlobal = new("EncryptedGlobal");

    /// <summary>
    /// Convert from a <see cref="string"/> to <see cref="ComplianceMetadataType"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    public static implicit operator ComplianceMetadataType(string value) => new(value);

    /// <summary>
    /// Convert from <see cref="ComplianceMetadataType"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="ComplianceMetadataType"/> to convert from.</param>
    public static implicit operator string(ComplianceMetadataType value) => value.Value;
}
