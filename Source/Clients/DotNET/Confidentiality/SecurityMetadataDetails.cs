// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Confidentiality;

/// <summary>
/// Represents additional details about a piece of <see cref="SecurityMetadata"/> - why or to what purpose/extent
/// the type or property marked needs the protection its <see cref="SecurityMetadataType"/> describes.
/// </summary>
/// <param name="Value">Underlying value.</param>
public record SecurityMetadataDetails(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Convert from a <see cref="string"/> to <see cref="SecurityMetadataDetails"/>.
    /// </summary>
    /// <param name="value"><see cref="string"/> to convert from.</param>
    public static implicit operator SecurityMetadataDetails(string value) => new(value);

    /// <summary>
    /// Convert from <see cref="SecurityMetadataDetails"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="SecurityMetadataDetails"/> to convert from.</param>
    public static implicit operator string(SecurityMetadataDetails value) => value.Value;
}
