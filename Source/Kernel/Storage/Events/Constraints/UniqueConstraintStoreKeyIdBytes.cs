// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Converts a <see cref="UniqueConstraintStoreKeyId"/> to and from its canonical 16 byte form, shared by all providers.
/// </summary>
public static class UniqueConstraintStoreKeyIdBytes
{
    /// <summary>
    /// The size of the binary key identity in bytes.
    /// </summary>
    public const int Size = 16;

    /// <summary>
    /// Converts an identity to its 16 bytes.
    /// </summary>
    /// <param name="id">The identity, which must be exactly 32 lowercase hexadecimal characters.</param>
    /// <returns>The identity bytes.</returns>
    /// <exception cref="InvalidUniqueConstraintStoreKeyId">The identity is not canonical.</exception>
    public static byte[] From(UniqueConstraintStoreKeyId id)
    {
        var value = id?.Value;
        if (value is null || value.Length != Size * 2 || !value.All(IsLowerHex))
        {
            throw new InvalidUniqueConstraintStoreKeyId(value?.Length ?? 0);
        }

        return Convert.FromHexString(value);
    }

    /// <summary>
    /// Converts 16 bytes to the canonical identity.
    /// </summary>
    /// <param name="bytes">The identity bytes, which must be exactly 16 bytes.</param>
    /// <returns>The canonical lowercase identity.</returns>
    /// <exception cref="InvalidUniqueConstraintStoreKeyId">The input is not 16 bytes.</exception>
    public static UniqueConstraintStoreKeyId To(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Size)
        {
            throw new InvalidUniqueConstraintStoreKeyId(bytes.Length * 2);
        }

        return new UniqueConstraintStoreKeyId(Convert.ToHexStringLower(bytes));
    }

    static bool IsLowerHex(char c) => c is (>= '0' and <= '9') or (>= 'a' and <= 'f');
}
