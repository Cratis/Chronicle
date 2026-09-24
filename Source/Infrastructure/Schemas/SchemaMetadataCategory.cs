// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents the domain a piece of schema metadata belongs to - what a value is marked for, not how it is
/// technically stored.
/// </summary>
/// <param name="Value">Underlying value.</param>
/// <remarks>
/// A JSON schema node can carry metadata for more than one, unrelated reason: <c language="csharp">[PII]</c> exists
/// to satisfy GDPR - it is about who a value is about and their right to erasure. <c language="csharp">[Encrypted]</c>
/// exists to keep a value confidential at rest - it is a security measure with no data subject and no erasure path
/// at all. The two answer different questions and are governed by different rules, so they are stored under
/// different schema keys (see the category-aware overloads on the schema metadata extensions) rather than sharing
/// one metadata bucket that a reader has to disambiguate after the fact. This concept is the discriminator that
/// keeps them apart through the shared, generalized machinery that reads and writes schema metadata for either one.
/// </remarks>
public record SchemaMetadataCategory(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Metadata governed by compliance rules - GDPR and similar regimes concerned with personal data and the
    /// rights of the person it is about.
    /// </summary>
    public static readonly SchemaMetadataCategory Compliance = new(nameof(Compliance));

    /// <summary>
    /// Metadata governed by security rules - confidentiality measures with no data subject and no erasure path.
    /// </summary>
    public static readonly SchemaMetadataCategory Security = new(nameof(Security));

    /// <summary>
    /// Convert from a <see cref="string"/> to <see cref="SchemaMetadataCategory"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    public static implicit operator SchemaMetadataCategory(string value) => new(value);

    /// <summary>
    /// Convert from <see cref="SchemaMetadataCategory"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="SchemaMetadataCategory"/> to convert from.</param>
    public static implicit operator string(SchemaMetadataCategory value) => value.Value;
}
