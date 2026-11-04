// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Hashes identifiers with a deployment key for correlation without exposing their values.
/// </summary>
/// <param name="key">The deployment key.</param>
/// <exception cref="InvalidIdentifierHashKey">The key is missing or too short.</exception>
public sealed class IdentifierHasher(string key)
{
    /// <summary>
    /// The minimum number of characters required in a deployment key.
    /// </summary>
    public const int MinimumKeyLength = 32;

    const string HexDigits = "0123456789abcdef";

    readonly byte[] _key = GetKeyBytes(key);

    /// <summary>
    /// Hash an identifier using HMAC-SHA256 truncated to 128 bits.
    /// </summary>
    /// <param name="value">The identifier.</param>
    /// <returns>The 32-character lowercase hexadecimal hash.</returns>
    public string Hash(string value)
    {
        Span<char> hash = stackalloc char[32];
        TryHash(value.AsSpan(), hash, out _);
        return hash.ToString();
    }

    /// <summary>
    /// Try to hash an identifier into a destination span.
    /// </summary>
    /// <param name="value">The identifier.</param>
    /// <param name="destination">The destination for the lowercase hexadecimal hash.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the destination was large enough for the hash.</returns>
    public bool TryHash(ReadOnlySpan<char> value, Span<char> destination, out int written)
    {
        written = 0;
        if (destination.Length < 32) return false;

        var byteCount = Encoding.UTF8.GetByteCount(value);
        var bytes = byteCount <= 1024 ? stackalloc byte[byteCount] : new byte[byteCount];
        Encoding.UTF8.GetBytes(value, bytes);
        Span<byte> hash = stackalloc byte[32];
        HMACSHA256.HashData(_key, bytes, hash);
        for (var index = 0; index < 16; index++)
        {
            destination[index * 2] = HexDigits[hash[index] >> 4];
            destination[(index * 2) + 1] = HexDigits[hash[index] & 15];
        }

        written = 32;
        return true;
    }

    static byte[] GetKeyBytes(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < MinimumKeyLength)
        {
            throw new InvalidIdentifierHashKey();
        }

        return Encoding.UTF8.GetBytes(key);
    }
}
