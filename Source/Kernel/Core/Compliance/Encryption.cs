// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Releases;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// Protects values using fresh AES-256-GCM data keys wrapped with a managed RSA key.
/// </summary>
/// <remarks>
/// Reads v1, revision-aware v2, signed-identity v3 and typed v4 CENV envelopes, and legacy raw RSA PKCS#1 values.
/// Typed writes use v4 locally or after <see cref="TypedProtectedValuesFloor"/>; earlier silos cannot read v4.
/// A v3 envelope is written only by <see cref="Encrypt(byte[], EncryptionKey, EncryptionKeyRevision, ProtectedValueAddress)"/>, and only once
/// <see cref="KeyIdentityFloor"/> is reached, so no silo that cannot read v3 is running when it is first written. Every other write is v1.
/// Downgrade after v3 writes is unsupported: an older reader fails closed on version 3.
/// </remarks>
public class Encryption : IEncryption
{
    const int KeySize = 2048;
    const int DataKeySizeInBytes = 32;
    const int LegacyCiphertextSizeInBytes = KeySize / 8;
    static readonly RSAEncryptionPadding _keyWrapPadding = RSAEncryptionPadding.OaepSHA256;
    static readonly EncryptionKeyReserve _reserve = new(GenerateKeyPair, Environment.ProcessorCount);
    readonly IClusterReleases? _releases;
    readonly EnvelopeSigningKeys _signingKeys;

    /// <summary>
    /// Initializes a local writer: typed values use v4, while untyped overloads retain v1 behavior.
    /// </summary>
    public Encryption()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Encryption"/> class.
    /// </summary>
    /// <param name="releases">The <see cref="IClusterReleases"/> that gates v3 writes, or null to keep writing v1.</param>
    public Encryption(IClusterReleases? releases)
        : this(releases, EnvelopeSigningKeys.Shared)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Encryption"/> class with an explicit cache of certified signing keys.
    /// </summary>
    /// <param name="releases">The <see cref="IClusterReleases"/> that gates v3 writes, or null to keep writing v1.</param>
    /// <param name="signingKeys">The cache of certified signing keys.</param>
    internal Encryption(IClusterReleases? releases, EnvelopeSigningKeys signingKeys)
    {
        _releases = releases;
        _signingKeys = signingKeys;
    }

    /// <inheritdoc/>
    public bool SupportsTypedValues => _releases is null || TypedProtectedValuesFloor.IsReached(_releases);

    /// <inheritdoc/>
    public EncryptionKey GenerateKey() => _reserve.Take();

    /// <inheritdoc/>
    public byte[] Encrypt(byte[] bytes, EncryptionKey key, EncryptionKeyRevision revision) => EncryptEnvelope(bytes, new(key, revision), 1);

    /// <inheritdoc/>
    public byte[] Encrypt(byte[] bytes, EncryptionKey key, EncryptionKeyRevision revision, ProtectedValueAddress address) =>
        KeyIdentityFloor.IsReached(_releases)
            ? EncryptSigned(bytes, new(key, revision), address)
            : EncryptEnvelope(bytes, new(key, revision), 1);

    /// <inheritdoc/>
    public byte[] Encrypt(byte[] bytes, EncryptionKeyAtRevision key, ProtectedValueAddress address, ProtectedValueKind kind)
    {
        if (!SupportsTypedValues)
        {
            throw new NotSupportedException("The cluster has not reached the authenticated JSON kind write floor.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return EncryptSigned(bytes, key, address, kind);
    }

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

    byte[] EncryptSigned(byte[] bytes, EncryptionKeyAtRevision pair, ProtectedValueAddress address, ProtectedValueKind? kind = null)
    {
        var commitment = EnvelopeSigning.Commit(address);
        using var signing = _signingKeys.Begin(pair.Key, pair.Revision, commitment);

        // Never cache a data key: a fresh key per value prevents nonce reuse under the same AES key.
        var dataKey = RandomNumberGenerator.GetBytes(DataKeySizeInBytes);
        try
        {
            var wrappedKey = WrapDataKey(dataKey, pair.Key);
            while (true)
            {
                var signer = signing.Acquire();
                var envelope = new EncryptionEnvelope
                {
                    Header = EncryptionEnvelope.CreateSignedHeader(pair.Revision, (pair.Key.Public, signer.Certificate, signer.SigningKey), wrappedKey.Length, kind),
                    PublicKey = pair.Key.Public,
                    Certificate = signer.Certificate,
                    SigningKey = signer.SigningKey,
                    WrappedKey = wrappedKey,
                    Nonce = RandomNumberGenerator.GetBytes(EncryptionEnvelope.NonceSize),
                    Tag = new byte[EncryptionEnvelope.TagSize],
                    Ciphertext = new byte[bytes.Length],
                    Revision = pair.Revision
                };
                using var aes = new AesGcm(dataKey, EncryptionEnvelope.TagSize);
                aes.Encrypt(envelope.Nonce, bytes, envelope.Ciphertext, envelope.Tag, envelope.AssociatedData);

                // Ordinary invalidation retries; erasure revokes this write's operation, so its next acquisition fails.
                if (signer.TrySign(EnvelopeSigning.ValueMessage(envelope, commitment), out var signature))
                {
                    return (envelope with { Signature = signature }).Bytes();
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dataKey);
        }
    }
}
