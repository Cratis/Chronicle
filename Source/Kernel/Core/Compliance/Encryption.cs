// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Represents an implementation of <see cref="IEncryption"/>.
/// </summary>
/// <remarks>
/// PII values are protected with hybrid (envelope) encryption: every value is encrypted with a fresh
/// AES-256-GCM data key, and only that 32-byte data key is RSA-OAEP-wrapped with the per-subject key. This
/// removes the ~245-byte plaintext cap that raw RSA-2048 imposes, so arbitrarily large PII (long free
/// text, images) can be encrypted while the per-subject RSA key remains the root of trust for
/// crypto-shredding. Values written by earlier versions were encrypted with raw RSA over the whole
/// payload; <see cref="Decrypt"/> recognizes the envelope header (the <c>CENV</c> "Cratis ENVelope"
/// marker followed by a format-version byte) and falls back to raw RSA for legacy values without the
/// marker, so previously stored PII stays readable.
/// </remarks>
public class Encryption : IEncryption
{
    const int KeySize = 2048;
    const int DataKeySizeInBytes = 32;
    const int NonceSizeInBytes = 12;
    const int TagSizeInBytes = 16;
    const byte EnvelopeVersion = 1;

    static readonly byte[] _envelopeMagic = "CENV"u8.ToArray();
    static readonly RSAEncryptionPadding _keyWrapPadding = RSAEncryptionPadding.OaepSHA256;

    /// <inheritdoc/>
    public EncryptionKey GenerateKey()
    {
        using var rsa = RSA.Create(KeySize);
        var privateKey = rsa.ExportRSAPrivateKey();
        var publicKey = rsa.ExportRSAPublicKey();
        return new(publicKey, privateKey);
    }

    /// <inheritdoc/>
    public byte[] Encrypt(byte[] bytes, EncryptionKey key)
    {
        // A fresh AES data key is generated per value, so the random nonce below can never collide under the
        // same key — the catastrophic AES-GCM (key, nonce) reuse failure mode cannot occur here. Preserve
        // this invariant: never cache or reuse a data key across values.
        var dataKey = RandomNumberGenerator.GetBytes(DataKeySizeInBytes);
        try
        {
            var nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
            var tag = new byte[TagSizeInBytes];
            var ciphertext = new byte[bytes.Length];

            using (var aes = new AesGcm(dataKey, TagSizeInBytes))
            {
                aes.Encrypt(nonce, bytes, ciphertext, tag);
            }

            var wrappedKey = WrapDataKey(dataKey, key);
            return ComposeEnvelope(wrappedKey, nonce, tag, ciphertext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }
    }

    /// <inheritdoc/>
    public byte[] Decrypt(byte[] bytes, EncryptionKey key)
    {
        if (!TryReadEnvelope(bytes, out var wrappedKey, out var nonce, out var tag, out var ciphertext))
        {
            return DecryptLegacy(bytes, key);
        }

        var dataKey = UnwrapDataKey(wrappedKey, key);
        try
        {
            var plaintext = new byte[ciphertext.Length];

            using (var aes = new AesGcm(dataKey, TagSizeInBytes))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
            }

            return plaintext;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }
    }

    static byte[] WrapDataKey(byte[] dataKey, EncryptionKey key)
    {
        // The data key is wrapped with RSA-OAEP (SHA-256) — the modern padding for this new format. Legacy
        // values (see DecryptLegacy) were written with raw RSA PKCS#1 v1.5 and are read back with that padding.
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(key.Public, out _);
        return rsa.Encrypt(dataKey, _keyWrapPadding);
    }

    static byte[] UnwrapDataKey(byte[] wrappedKey, EncryptionKey key)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(key.Private, out _);
        return rsa.Decrypt(wrappedKey, _keyWrapPadding);
    }

    static byte[] DecryptLegacy(byte[] bytes, EncryptionKey key)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(key.Private, out _);
        return rsa.Decrypt(bytes, RSAEncryptionPadding.Pkcs1);
    }

    static byte[] ComposeEnvelope(byte[] wrappedKey, byte[] nonce, byte[] tag, byte[] ciphertext)
    {
        var headerSize = _envelopeMagic.Length + 1 + sizeof(ushort);
        var result = new byte[headerSize + wrappedKey.Length + nonce.Length + tag.Length + ciphertext.Length];
        var span = result.AsSpan();

        var offset = 0;
        _envelopeMagic.CopyTo(span);
        offset += _envelopeMagic.Length;

        span[offset++] = EnvelopeVersion;

        BinaryPrimitives.WriteUInt16BigEndian(span[offset..], (ushort)wrappedKey.Length);
        offset += sizeof(ushort);

        wrappedKey.CopyTo(span[offset..]);
        offset += wrappedKey.Length;

        nonce.CopyTo(span[offset..]);
        offset += nonce.Length;

        tag.CopyTo(span[offset..]);
        offset += tag.Length;

        ciphertext.CopyTo(span[offset..]);

        return result;
    }

    static bool TryReadEnvelope(byte[] bytes, out byte[] wrappedKey, out byte[] nonce, out byte[] tag, out byte[] ciphertext)
    {
        wrappedKey = nonce = tag = ciphertext = [];

        var headerSize = _envelopeMagic.Length + 1 + sizeof(ushort);
        if (bytes.Length < headerSize)
        {
            return false;
        }

        var span = bytes.AsSpan();
        if (!span[.._envelopeMagic.Length].SequenceEqual(_envelopeMagic))
        {
            return false;
        }

        var offset = _envelopeMagic.Length;
        if (span[offset++] != EnvelopeVersion)
        {
            return false;
        }

        var wrappedKeyLength = BinaryPrimitives.ReadUInt16BigEndian(span[offset..]);
        offset += sizeof(ushort);

        if (bytes.Length < offset + wrappedKeyLength + NonceSizeInBytes + TagSizeInBytes)
        {
            return false;
        }

        wrappedKey = span.Slice(offset, wrappedKeyLength).ToArray();
        offset += wrappedKeyLength;

        nonce = span.Slice(offset, NonceSizeInBytes).ToArray();
        offset += NonceSizeInBytes;

        tag = span.Slice(offset, TagSizeInBytes).ToArray();
        offset += TagSizeInBytes;

        ciphertext = span[offset..].ToArray();

        return true;
    }
}
