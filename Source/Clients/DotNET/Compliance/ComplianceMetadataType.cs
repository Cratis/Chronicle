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
