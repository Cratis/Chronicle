// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Protects values using fresh AES-256-GCM data keys wrapped with a managed RSA key.
/// </summary>
/// <remarks>
/// Reads v1 and revision-aware v2 CENV envelopes, and legacy raw RSA PKCS#1 values.
/// Writes remain v1 until all production replicas can read v2. Downgrade after enabling v2 writes is unsupported.
/// </remarks>
public class Encryption : IEncryption
{
    /// <summary>Reader-first rollout: flip only after every production replica supports v2 reads.</summary>
    internal const byte WriterVersion = 1;
    const int KeySize = 2048;
    const int DataKeySizeInBytes = 32;
    const int LegacyCiphertextSizeInBytes = KeySize / 8;
    static readonly RSAEncryptionPadding _keyWrapPadding = RSAEncryptionPadding.OaepSHA256;
    static readonly EncryptionKeyReserve _reserve = new(GenerateKeyPair, Environment.ProcessorCount);

    /// <inheritdoc/>
    public EncryptionKey GenerateKey() => _reserve.Take();

    /// <inheritdoc/>
    public byte[] Encrypt(byte[] bytes, EncryptionKey key, EncryptionKeyRevision revision) => EncryptEnvelope(bytes, new(key, revision), WriterVersion);

    /// <inheritdoc/>
    public bool IsEncrypted(byte[] bytes) => EncryptionEnvelope.HasMagic(bytes) || bytes.Length == LegacyCiphertextSizeInBytes;

    /// <inheritdoc/>
    public bool TryGetKeyRevision(byte[] bytes, out EncryptionKeyRevision revision)
    {
        var recorded = EncryptionEnvelope.HasMagic(bytes) ? EncryptionEnvelope.Read(bytes).Revision : null;
        revision = recorded ?? EncryptionKeyRevision.Initial;
        return recorded is not null;
    }

    /// <inheritdoc/>
    /// <exception cref="EncryptionKeyUnwrapFailed">The imported key cannot decrypt the RSA-protected value.</exception>
    public byte[] Decrypt(byte[] bytes, EncryptionKey key)
    {
        if (!EncryptionEnvelope.HasMagic(bytes))
        {
            return DecryptRsa(bytes, key, RSAEncryptionPadding.Pkcs1);
        }

        var envelope = EncryptionEnvelope.Read(bytes);
        var dataKey = DecryptRsa(envelope.WrappedKey, key, _keyWrapPadding);
        try
        {
            var plaintext = new byte[envelope.Ciphertext.Length];
            using var aes = new AesGcm(dataKey, EncryptionEnvelope.TagSize);
            aes.Decrypt(envelope.Nonce, envelope.Ciphertext, envelope.Tag, plaintext, envelope.AssociatedData);
            return plaintext;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }
    }

    /// <summary>Encrypts a value in an explicitly selected envelope version.</summary>
    /// <param name="bytes">The plaintext bytes.</param>
    /// <param name="key">The managed key and its concrete revision.</param>
    /// <param name="version">The envelope version.</param>
    /// <returns>The encrypted envelope.</returns>
    internal static byte[] EncryptEnvelope(byte[] bytes, EncryptionKeyAtRevision key, byte version)
    {
        // Never cache a data key: a fresh key per value prevents nonce reuse under the same AES key.
        var dataKey = RandomNumberGenerator.GetBytes(DataKeySizeInBytes);
        try
        {
            var wrappedKey = WrapDataKey(dataKey, key.Key);
            var envelope = new EncryptionEnvelope
            {
                Header = EncryptionEnvelope.CreateHeader(version, key.Revision, wrappedKey.Length),
                WrappedKey = wrappedKey,
                Nonce = RandomNumberGenerator.GetBytes(EncryptionEnvelope.NonceSize),
                Tag = new byte[EncryptionEnvelope.TagSize],
                Ciphertext = new byte[bytes.Length],
                Revision = version == 2 ? key.Revision : null
            };
            using var aes = new AesGcm(dataKey, EncryptionEnvelope.TagSize);
            aes.Encrypt(envelope.Nonce, bytes, envelope.Ciphertext, envelope.Tag, envelope.AssociatedData);
            return envelope.Bytes();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }
    }

    static byte[] WrapDataKey(byte[] dataKey, EncryptionKey key)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(key.Public, out _);
        return rsa.Encrypt(dataKey, _keyWrapPadding);
    }

    static byte[] DecryptRsa(byte[] bytes, EncryptionKey key, RSAEncryptionPadding padding)
    {
        using var rsa = RSA.Create();

        // Invalid key material is an operational failure, not a reason to try another revision.
        rsa.ImportRSAPrivateKey(key.Private, out _);
        try
        {
            return rsa.Decrypt(bytes, padding);
        }
        catch (CryptographicException ex)
        {
            throw new EncryptionKeyUnwrapFailed(ex);
        }
    }

    static EncryptionKey GenerateKeyPair()
    {
        using var rsa = RSA.Create(KeySize);
        return new(rsa.ExportRSAPublicKey(), rsa.ExportRSAPrivateKey());
    }
}
