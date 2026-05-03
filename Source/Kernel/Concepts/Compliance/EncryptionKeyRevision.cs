// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Represents the revision of an <see cref="EncryptionKeyIdentifier">encryption key</see>.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record EncryptionKeyRevision(uint Value) : ConceptAs<uint>(Value)
{
    /// <summary>
    /// Gets the revision that represents the latest revision.
    /// </summary>
    public static readonly EncryptionKeyRevision Latest = uint.MaxValue;

    /// <summary>
    /// Implicitly convert from <see cref="uint"/> to <see cref="EncryptionKeyRevision"/>.
    /// </summary>
    /// <param name="value"><see cref="uint"/> to convert from.</param>
    public static implicit operator EncryptionKeyRevision(uint value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="EncryptionKeyRevision"/> to <see cref="uint"/>.
    /// </summary>
    /// <param name="revision"><see cref="EncryptionKeyRevision"/> to convert from.</param>
    public static implicit operator uint(EncryptionKeyRevision revision) => revision.Value;
}
