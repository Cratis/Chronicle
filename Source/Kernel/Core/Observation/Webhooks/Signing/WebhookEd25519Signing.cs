// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Signs and verifies caller-owned bytes using the Standard Webhooks Ed25519 scheme.
/// </summary>
internal static class WebhookEd25519Signing
{
    /// <summary>
    /// Frames and signs the exact payload bytes.
    /// </summary>
    /// <param name="messageId">The caller-owned identifier, without framing delimiters.</param>
    /// <param name="unixTimestamp">The caller-owned attempt time in Unix seconds.</param>
    /// <param name="payload">The exact bytes to be delivered.</param>
    /// <param name="seed">The raw 32-byte RFC 8032 private seed.</param>
    /// <returns>A single v1a signature suitable for the webhook-signature header.</returns>
    /// <exception cref="InvalidWebhookMessageId">The identifier contains a framing delimiter.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">The identifier cannot be encoded losslessly as UTF-8.</exception>
    internal static string Sign(string messageId, long unixTimestamp, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> seed)
    {
        var message = WebhookSigning.Frame(messageId, unixTimestamp, payload);
        return $"v1a,{Convert.ToBase64String(ComputeSignature(message, seed))}";
    }

    /// <summary>
    /// Computes the Ed25519 signature independently of framing or header formatting.
    /// </summary>
    /// <param name="message">The bytes to authenticate.</param>
    /// <param name="seed">The raw 32-byte RFC 8032 private seed.</param>
    /// <returns>The 64-byte Ed25519 signature.</returns>
    internal static byte[] ComputeSignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> seed)
    {
        var signer = new Ed25519Signer();
        signer.Init(true, new Ed25519PrivateKeyParameters(seed));
        signer.BlockUpdate(message);
        return signer.GenerateSignature();
    }

    /// <summary>
    /// Verifies an Ed25519 signature, rejecting invalid lengths and public points.
    /// </summary>
    /// <param name="message">The bytes to authenticate.</param>
    /// <param name="signature">The raw 64-byte signature.</param>
    /// <param name="publicKey">The raw 32-byte public key.</param>
    /// <returns>True only when the signature is valid for the message and public key.</returns>
    internal static bool VerifySignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
    {
        if (signature.Length != 64 || publicKey.Length != 32)
        {
            return false;
        }

        try
        {
            var verifier = new Ed25519Signer();
            verifier.Init(false, new Ed25519PublicKeyParameters(publicKey));
            verifier.BlockUpdate(message);
            return verifier.VerifySignature(signature.ToArray());
        }
        catch (ArgumentException)
        {
            // Invalid point encodings are untrusted input, not a verification failure to propagate.
            return false;
        }
    }

    /// <summary>
    /// Derives the raw public key from an RFC 8032 private seed.
    /// </summary>
    /// <param name="seed">The raw 32-byte private seed.</param>
    /// <returns>The raw 32-byte public key.</returns>
    internal static byte[] DerivePublicKey(ReadOnlySpan<byte> seed) => new Ed25519PrivateKeyParameters(seed).GeneratePublicKey().GetEncoded();

    /// <summary>
    /// Generates a private seed using the system cryptographic random number generator.
    /// </summary>
    /// <returns>A new 32-byte private seed.</returns>
    internal static byte[] GenerateSeed() => RandomNumberGenerator.GetBytes(32);
}
