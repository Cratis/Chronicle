// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents the name of a metadata type within a <see cref="SchemaMetadataCategory"/> - for example
/// <c language="csharp">PII</c> or <c language="csharp">EncryptedSubject</c>.
/// </summary>
/// <param name="Value">Underlying value.</param>
/// <remarks>
/// Only has to be unique within its <see cref="SchemaMetadataCategory"/>, not globally - see
/// <see cref="SchemaMetadataCategory"/> for why a compliance handler and a security handler can never answer for
/// each other's key even if they happened to share a type name.
/// </remarks>
public record SchemaMetadataTypeName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Convert from a <see cref="string"/> to <see cref="SchemaMetadataTypeName"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    public static implicit operator SchemaMetadataTypeName(string value) => new(value);

    /// <summary>
    /// Convert from <see cref="SchemaMetadataTypeName"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="SchemaMetadataTypeName"/> to convert from.</param>
    public static implicit operator string(SchemaMetadataTypeName value) => value.Value;
}
