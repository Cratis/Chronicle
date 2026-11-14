// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Cratis.Chronicle.Compliance;

/// <summary>The parsed, authenticated hybrid encryption envelope.</summary>
internal sealed record EncryptionEnvelope
{
    /// <summary>The AES-GCM nonce size in bytes.</summary>
    internal const int NonceSize = 12;

    /// <summary>The AES-GCM authentication tag size in bytes.</summary>
    internal const int TagSize = 16;
    const int RevisionOffset = 5;
    static readonly byte[] _magic = "CENV"u8.ToArray();

    /// <summary>Gets the serialized header.</summary>
    internal required byte[] Header { get; init; }

    /// <summary>Gets the wrapped per-value data key.</summary>
    internal required byte[] WrappedKey { get; init; }

    /// <summary>Gets the per-value nonce.</summary>
    internal required byte[] Nonce { get; init; }

    /// <summary>Gets the authentication tag.</summary>
    internal required byte[] Tag { get; init; }

    /// <summary>Gets the encrypted payload.</summary>
    internal required byte[] Ciphertext { get; init; }

    /// <summary>Gets the recorded key revision, if present.</summary>
    internal EncryptionKeyRevision? Revision { get; init; }

    /// <summary>Gets the v2 authenticated header, absent in v1.</summary>
    internal byte[]? AssociatedData => Revision is null ? null : Header;

    /// <summary>Recognizes prefixed values, including malformed or unsupported versions.</summary>
    /// <param name="bytes">The candidate value.</param>
    /// <returns>Whether the value has the envelope prefix.</returns>
    internal static bool HasMagic(byte[] bytes) => bytes.AsSpan().StartsWith(_magic);

    /// <summary>Creates a v1 or v2 header.</summary>
    /// <param name="version">The envelope version.</param>
    /// <param name="revision">The concrete managed-key revision.</param>
    /// <param name="wrappedKeyLength">The size of the wrapped data key.</param>
    /// <returns>The serialized header.</returns>
    internal static byte[] CreateHeader(byte version, EncryptionKeyRevision revision, int wrappedKeyLength)
    {
        var header = new byte[version == 2 ? 11 : 7];
        _magic.CopyTo(header, 0);
        header[4] = version;
        var lengthOffset = RevisionOffset;
        if (version == 2)
        {
            BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(RevisionOffset), revision.Value);
            lengthOffset += sizeof(uint);
        }

        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(lengthOffset), checked((ushort)wrappedKeyLength));
        return header;
    }

    /// <summary>Reads a prefixed envelope without falling back to raw RSA on malformed input.</summary>
    /// <param name="bytes">The prefixed value.</param>
    /// <returns>The parsed envelope.</returns>
    internal static EncryptionEnvelope Read(byte[] bytes)
    {
        if (bytes.Length < 7 || bytes[4] is not (1 or 2))
        {
            throw new CryptographicException("Invalid encryption envelope header.");
        }

        var headerLength = bytes[4] == 2 ? 11 : 7;
        if (bytes.Length < headerLength)
        {
            throw new CryptographicException("Truncated encryption envelope header.");
        }

        var header = bytes[..headerLength];
        var wrappedKeyLength = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(headerLength - sizeof(ushort)));
        var payloadOffset = headerLength + wrappedKeyLength;
        if (wrappedKeyLength == 0 || bytes.Length < payloadOffset + NonceSize + TagSize)
        {
            throw new CryptographicException("Truncated encryption envelope payload.");
        }

        return new()
        {
            Header = header,
            WrappedKey = bytes[headerLength..payloadOffset],
            Nonce = bytes[payloadOffset..(payloadOffset + NonceSize)],
            Tag = bytes[(payloadOffset + NonceSize)..(payloadOffset + NonceSize + TagSize)],
            Ciphertext = bytes[(payloadOffset + NonceSize + TagSize)..],
            Revision = ReadRevision(header)
        };
    }

    /// <summary>Serializes the complete envelope.</summary>
    /// <returns>The header and encrypted payload.</returns>
    internal byte[] Bytes() => [.. Header, .. WrappedKey, .. Nonce, .. Tag, .. Ciphertext];

    static EncryptionKeyRevision? ReadRevision(byte[] header)
    {
        if (header[4] == 1) return null;
        var revision = new EncryptionKeyRevision(BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(RevisionOffset)));
        if (revision == EncryptionKeyRevision.Latest || revision.Value == 0)
        {
            throw new CryptographicException("Invalid encryption key revision.");
        }

        return revision;
    }
}
