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
