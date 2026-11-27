// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// The canonical messages and algorithms behind the signed identity of a v3 envelope.
/// </summary>
/// <remarks>
/// Every message is a domain-separation label followed by length-prefixed fields; nothing is concatenated raw.
/// The managed RSA key signs a certificate with RSA-PSS SHA-256. The certified ephemeral ECDSA P-256 key signs the whole
/// envelope with SHA-256 in IEEE P1363 format. Both signatures cover a commitment to the address the value was written under.
/// </remarks>
internal static class EnvelopeSigning
{
    const string AddressLabel = "cratis-chronicle/cenv-address/v3";
    const string CertificateLabel = "cratis-chronicle/cenv-cert/v3";
    const string ValueLabel = "cratis-chronicle/cenv-value/v3";
    const int EcdsaKeySize = 256;

    /// <summary>Gets the signature algorithm of the value signature.</summary>
    internal static DSASignatureFormat ValueSignatureFormat => DSASignatureFormat.IeeeP1363FixedFieldConcatenation;

    /// <summary>Commits to a key-storage address.</summary>
    /// <param name="address">The address.</param>
    /// <returns>The commitment. It is not confidential: it can be tested against a guessed address offline.</returns>
    internal static byte[] Commit(ProtectedValueAddress address) => SHA256.HashData(Frame(
        AddressLabel,
        Encoding.UTF8.GetBytes(address.EventStore.Value),
        Encoding.UTF8.GetBytes(address.Namespace.Value),
        Encoding.UTF8.GetBytes(address.Identifier.Value)));

    /// <summary>Gets the fingerprint of a public key, as <see cref="EncryptionKey.Fingerprint"/> defines it.</summary>
    /// <param name="publicKey">The public key bytes.</param>
    /// <returns>The lowercase hexadecimal SHA-256 of the key.</returns>
    internal static string Fingerprint(byte[] publicKey) => Convert.ToHexStringLower(SHA256.HashData(publicKey));

    /// <summary>Builds the message the managed RSA key signs to certify an ephemeral signing key.</summary>
    /// <param name="revision">The managed-key revision.</param>
    /// <param name="commitment">The address commitment.</param>
    /// <param name="publicKey">The managed RSA public key.</param>
    /// <param name="signingKey">The ephemeral ECDSA public key.</param>
    /// <returns>The canonical message.</returns>
    internal static byte[] CertificateMessage(EncryptionKeyRevision revision, byte[] commitment, byte[] publicKey, byte[] signingKey)
    {
        var revisionBytes = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(revisionBytes, revision.Value);
        return Frame(CertificateLabel, [3], revisionBytes, commitment, SHA256.HashData(publicKey), signingKey);
    }

    /// <summary>Builds the message the ephemeral key signs for a value.</summary>
    /// <param name="envelope">The envelope without its signature.</param>
    /// <param name="commitment">The address commitment.</param>
    /// <returns>The canonical message.</returns>
    internal static byte[] ValueMessage(EncryptionEnvelope envelope, byte[] commitment) =>
        Frame(ValueLabel, envelope.Header, commitment, envelope.WrappedKey, envelope.Nonce, envelope.Tag, envelope.Ciphertext);

    /// <summary>Certifies an ephemeral signing key with the managed RSA private key.</summary>
    /// <param name="key">The managed key.</param>
    /// <param name="message">The certificate message.</param>
    /// <returns>The RSA-PSS SHA-256 signature.</returns>
    internal static byte[] SignCertificate(EncryptionKey key, byte[] message)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(key.Private, out _);
        return rsa.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
    }

    /// <summary>Verifies both signatures of a v3 envelope at the address it is released from.</summary>
    /// <param name="envelope">The parsed envelope.</param>
    /// <param name="address">The address it is released from.</param>
    /// <returns>The fingerprint of the embedded managed public key.</returns>
    /// <exception cref="EncryptionEnvelopeBindingFailed">Either signature, or the embedded material, does not verify.</exception>
    internal static string Verify(EncryptionEnvelope envelope, ProtectedValueAddress address)
    {
        if (!CarriesSignedIdentity(envelope))
        {
            throw new EncryptionEnvelopeBindingFailed("the value does not carry a signed identity");
        }

        var commitment = Commit(address);
        try
        {
            VerifyCertificate(envelope, commitment);
            VerifyValueSignature(envelope, commitment);
        }
        catch (CryptographicException ex)
        {
            throw new EncryptionEnvelopeBindingFailed("the embedded key material is malformed", ex);
        }

        return Fingerprint(envelope.PublicKey!);
    }

    static bool CarriesSignedIdentity(EncryptionEnvelope envelope) =>
        envelope.Version is 3 or 4 && new object?[] { envelope.PublicKey, envelope.Certificate, envelope.SigningKey, envelope.Signature, envelope.Revision }.All(_ => _ is not null);

    static void VerifyCertificate(EncryptionEnvelope envelope, byte[] commitment)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(envelope.PublicKey!, out _);
        var certificateMessage = CertificateMessage(envelope.Revision!, commitment, envelope.PublicKey!, envelope.SigningKey!);
        if (!rsa.VerifyData(certificateMessage, envelope.Certificate!, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
        {
            throw new EncryptionEnvelopeBindingFailed("the certificate does not verify at this address");
        }
    }

    static void VerifyValueSignature(EncryptionEnvelope envelope, byte[] commitment)
    {
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(envelope.SigningKey!, out _);
        if (ecdsa.KeySize != EcdsaKeySize
            || !ecdsa.VerifyData(ValueMessage(envelope, commitment), envelope.Signature!, HashAlgorithmName.SHA256, ValueSignatureFormat))
        {
            throw new EncryptionEnvelopeBindingFailed("the value signature does not verify at this address");
        }
    }

    static byte[] Frame(string label, params byte[][] fields)
    {
        using var stream = new MemoryStream();
        Write(stream, Encoding.UTF8.GetBytes(label));
        foreach (var field in fields)
        {
            Write(stream, field);
        }

        return stream.ToArray();
    }

    static void Write(MemoryStream stream, byte[] field)
    {
        Span<byte> length = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)field.Length);
        stream.Write(length);
        stream.Write(field);
    }
}
