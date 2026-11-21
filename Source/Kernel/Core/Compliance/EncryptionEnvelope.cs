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

    /// <summary>The size of the ECDSA P-256 value signature in IEEE P1363 format.</summary>
    internal const int SignatureSize = 64;
    const int RevisionOffset = 5;
    const int SignedHeaderSize = 17;
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

    /// <summary>Gets the envelope version.</summary>
    internal byte Version => Header[4];

    /// <summary>Gets the embedded public RSA key of the managed key, present in v3 only.</summary>
    internal byte[]? PublicKey { get; init; }

    /// <summary>Gets the RSA-PSS certificate over the ephemeral signing key, present in v3 only.</summary>
    internal byte[]? Certificate { get; init; }

    /// <summary>Gets the ephemeral ECDSA public key the certificate vouches for, present in v3 only.</summary>
    internal byte[]? SigningKey { get; init; }

    /// <summary>Gets the ECDSA signature over the whole envelope, present in v3 only.</summary>
    internal byte[]? Signature { get; init; }

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

    /// <summary>Creates a v3 header: the fixed prefix followed by the embedded identity parts.</summary>
    /// <param name="revision">The concrete managed-key revision.</param>
    /// <param name="publicKey">The managed key public part.</param>
    /// <param name="certificate">The certificate over the signing key.</param>
    /// <param name="signingKey">The ephemeral signing public key.</param>
    /// <param name="wrappedKeyLength">The size of the wrapped data key.</param>
    /// <returns>The serialized header, which is the AES-GCM associated data.</returns>
    internal static byte[] CreateSignedHeader(EncryptionKeyRevision revision, byte[] publicKey, byte[] certificate, byte[] signingKey, int wrappedKeyLength)
    {
        var header = new byte[SignedHeaderSize];
        _magic.CopyTo(header, 0);
        header[4] = 3;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(RevisionOffset), revision.Value);
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(9), checked((ushort)publicKey.Length));
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(11), checked((ushort)certificate.Length));
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(13), checked((ushort)signingKey.Length));
        BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(15), checked((ushort)wrappedKeyLength));
        return [.. header, .. publicKey, .. certificate, .. signingKey];
    }

    /// <summary>Reads a prefixed envelope without falling back to raw RSA on malformed input.</summary>
    /// <param name="bytes">The prefixed value.</param>
    /// <returns>The parsed envelope.</returns>
    /// <exception cref="CryptographicException">The envelope header, payload, or key revision is invalid.</exception>
    internal static EncryptionEnvelope Read(byte[] bytes)
    {
        if (bytes.Length < 7 || bytes[4] is not (1 or 2 or 3))
        {
            throw new CryptographicException("Invalid encryption envelope header.");
        }

        if (bytes[4] == 3)
        {
            return ReadSigned(bytes);
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
    internal byte[] Bytes() => [.. Header, .. WrappedKey, .. Nonce, .. Tag, .. Ciphertext, .. Signature ?? []];

    static EncryptionEnvelope ReadSigned(byte[] bytes)
    {
        if (bytes.Length < SignedHeaderSize)
        {
            throw new CryptographicException("Truncated encryption envelope header.");
        }

        var publicKeyLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(9));
        var certificateLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(11));
        var signingKeyLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(13));
        var wrappedKeyLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(15));
        if (publicKeyLength == 0 || certificateLength == 0 || signingKeyLength == 0 || wrappedKeyLength == 0)
        {
            throw new CryptographicException("Invalid encryption envelope component length.");
        }

        var publicKeyOffset = SignedHeaderSize;
        var certificateOffset = publicKeyOffset + publicKeyLength;
        var signingKeyOffset = certificateOffset + certificateLength;
        var wrappedKeyOffset = signingKeyOffset + signingKeyLength;
        var nonceOffset = wrappedKeyOffset + wrappedKeyLength;
        var tagOffset = nonceOffset + NonceSize;
        var ciphertextOffset = tagOffset + TagSize;
        if (bytes.Length < ciphertextOffset + SignatureSize)
        {
            throw new CryptographicException("Truncated encryption envelope payload.");
        }

        var signatureOffset = bytes.Length - SignatureSize;
        var header = bytes[..wrappedKeyOffset];
        return new()
        {
            Header = header,
            PublicKey = bytes[publicKeyOffset..certificateOffset],
            Certificate = bytes[certificateOffset..signingKeyOffset],
            SigningKey = bytes[signingKeyOffset..wrappedKeyOffset],
            WrappedKey = bytes[wrappedKeyOffset..nonceOffset],
            Nonce = bytes[nonceOffset..tagOffset],
            Tag = bytes[tagOffset..ciphertextOffset],
            Ciphertext = bytes[ciphertextOffset..signatureOffset],
            Signature = bytes[signatureOffset..],
            Revision = ReadRevision(header)
        };
    }

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
