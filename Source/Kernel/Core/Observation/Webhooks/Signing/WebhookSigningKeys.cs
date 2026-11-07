// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Encodes and parses Standard Webhooks signing keys without exposing key material in diagnostics.
/// </summary>
internal static class WebhookSigningKeys
{
    /// <summary>
    /// The Standard Webhooks symmetric secret prefix.
    /// </summary>
    internal const string HmacSecretPrefix = "whsec_";

    /// <summary>
    /// The Standard Webhooks private seed prefix.
    /// </summary>
    internal const string PrivateKeyPrefix = "whsk_";

    /// <summary>
    /// The Standard Webhooks public key prefix.
    /// </summary>
    internal const string PublicKeyPrefix = "whpk_";

    /// <summary>
    /// Encodes a raw Ed25519 public key.
    /// </summary>
    /// <param name="publicKey">The raw 32-byte public key.</param>
    /// <returns>The whpk_ prefixed base64 key.</returns>
    internal static string EncodePublicKey(ReadOnlySpan<byte> publicKey) => $"{PublicKeyPrefix}{Convert.ToBase64String(publicKey)}";

    /// <summary>
    /// Encodes an RFC 8032 private seed, not an expanded private key.
    /// </summary>
    /// <param name="seed">The raw 32-byte private seed.</param>
    /// <returns>The whsk_ prefixed base64 seed.</returns>
    internal static string EncodePrivateKey(ReadOnlySpan<byte> seed) => $"{PrivateKeyPrefix}{Convert.ToBase64String(seed)}";

    /// <summary>
    /// Encodes a raw HMAC secret.
    /// </summary>
    /// <param name="secret">The raw 24 to 64-byte secret.</param>
    /// <returns>The whsec_ prefixed base64 secret.</returns>
    internal static string EncodeHmacSecret(ReadOnlySpan<byte> secret) => $"{HmacSecretPrefix}{Convert.ToBase64String(secret)}";

    /// <summary>
    /// Parses an encoded public key of exactly 32 bytes.
    /// </summary>
    /// <param name="value">The whpk_ prefixed base64 value.</param>
    /// <param name="publicKey">The decoded key, or an empty array on failure.</param>
    /// <returns>True when the prefix, base64 and length are valid.</returns>
    internal static bool TryParsePublicKey(string value, out byte[] publicKey) => TryParse(value, PublicKeyPrefix, (32, 32), out publicKey);

    /// <summary>
    /// Parses an encoded private seed of exactly 32 bytes.
    /// </summary>
    /// <param name="value">The whsk_ prefixed base64 value.</param>
    /// <param name="seed">The decoded seed, or an empty array on failure.</param>
    /// <returns>True when the prefix, base64 and length are valid.</returns>
    internal static bool TryParsePrivateKey(string value, out byte[] seed) => TryParse(value, PrivateKeyPrefix, (32, 32), out seed);

    /// <summary>
    /// Parses an encoded HMAC secret of 24 to 64 bytes.
    /// </summary>
    /// <param name="value">The whsec_ prefixed base64 value.</param>
    /// <param name="secret">The decoded secret, or an empty array on failure.</param>
    /// <returns>True when the prefix, base64 and length are valid.</returns>
    internal static bool TryParseHmacSecret(string value, out byte[] secret) => TryParse(value, HmacSecretPrefix, (24, 64), out secret);

    /// <summary>
    /// Generates an HMAC secret using the system cryptographic random number generator.
    /// </summary>
    /// <returns>A new 32-byte secret.</returns>
    internal static byte[] GenerateHmacSecret() => RandomNumberGenerator.GetBytes(32);

    static bool TryParse(string value, string prefix, (int Minimum, int Maximum) length, out byte[] key)
    {
        key = [];
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        Span<byte> decoded = stackalloc byte[length.Maximum];
        if (!Convert.TryFromBase64String(value[prefix.Length..], decoded, out var written) || written < length.Minimum)
        {
            CryptographicOperations.ZeroMemory(decoded);
            return false;
        }

        key = decoded[..written].ToArray();
        CryptographicOperations.ZeroMemory(decoded);
        return true;
    }
}
