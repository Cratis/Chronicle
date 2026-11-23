// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Storage.Events.Constraints.V2;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

/// <summary>
/// Builds bounded lookup names from strictly encoded, length-framed logical identities.
/// </summary>
internal static class V2Identity
{
    static readonly UTF8Encoding _utf8 = new(false, true);

    /// <summary>
    /// Gets the digest of complete, unambiguous UTF-8 frames. Digests never establish ownership.
    /// </summary>
    /// <param name="parts">The exact logical components.</param>
    /// <returns>A lowercase SHA-256 digest.</returns>
    public static string Digest(params string[] parts)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var part in parts)
        {
            var bytes = Encode(part);
            var length = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    /// <summary>
    /// Rejects text BSON cannot represent without replacement-character aliasing.
    /// </summary>
    /// <param name="value">The exact text.</param>
    /// <returns>The strict UTF-8 representation.</returns>
    /// <exception cref="InvalidUniqueConstraintV2Input">The text is null or contains invalid UTF-16.</exception>
    public static byte[] Encode(string value)
    {
        if (value is null) throw new InvalidUniqueConstraintV2Input("identity");
        try
        {
            return _utf8.GetBytes(value);
        }
        catch (EncoderFallbackException)
        {
            throw new InvalidUniqueConstraintV2Input("identity");
        }
    }
}
