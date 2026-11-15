// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Verifies a Standard Webhooks <c language="text">webhook-signature</c> header against trusted secrets and public keys.
/// </summary>
internal static class WebhookSignatureVerification
{
    const string HmacVersion = "v1";
    const string Ed25519Version = "v1a";

    /// <summary>
    /// Verifies that at least one signature in the header was produced by a trusted secret.
    /// </summary>
    /// <remarks>
    /// The <c language="text">Cratis-Key-Id</c> header is advisory: <paramref name="keyIdHeader"/> is accepted only to make that
    /// contract explicit and never influences the result. The caller owns the timestamp tolerance policy.
    /// The <c language="text">v1</c> (HMAC-SHA256) and <c language="text">v1a</c> (Ed25519) versions are supported;
    /// entries of any other version are ignored, as are entries that are not valid base64.
    /// </remarks>
    /// <param name="messageId">The <c language="text">webhook-id</c> value.</param>
    /// <param name="unixTimestamp">The <c language="text">webhook-timestamp</c> value in Unix seconds.</param>
    /// <param name="payload">The exact received payload bytes.</param>
    /// <param name="signatureHeader">The <c language="text">webhook-signature</c> header value: space-separated <c language="text">version,signature</c> entries.</param>
    /// <param name="trustedSecrets">The raw secrets the receiver trusts.</param>
    /// <param name="keyIdHeader">The advisory <c language="text">Cratis-Key-Id</c> header value; ignored.</param>
    /// <returns>True when any entry is a valid signature from a trusted secret; otherwise false.</returns>
    /// <exception cref="InvalidWebhookMessageId">The message identifier contains a framing delimiter; the delivery cannot be verified and must be rejected.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">The message identifier cannot be encoded losslessly as UTF-8.</exception>
    internal static bool Verify(
        string messageId,
        long unixTimestamp,
        ReadOnlySpan<byte> payload,
        string? signatureHeader,
        IEnumerable<ReadOnlyMemory<byte>> trustedSecrets,
        string? keyIdHeader) => Verify(messageId, unixTimestamp, payload, signatureHeader, trustedSecrets, [], keyIdHeader);

    /// <summary>
    /// Verifies that at least one v1 or v1a entry was produced by a trusted secret or public key.
    /// </summary>
    /// <remarks>
    /// Unknown versions and malformed entries are ignored. The key-id header is advisory and never selects keys.
    /// Timestamp tolerance is the caller's policy.
    /// </remarks>
    /// <param name="messageId">The <c language="text">webhook-id</c> value.</param>
    /// <param name="unixTimestamp">The <c language="text">webhook-timestamp</c> value in Unix seconds.</param>
    /// <param name="payload">The exact received payload bytes.</param>
    /// <param name="signatureHeader">The space-separated <c language="text">version,signature</c> entries.</param>
    /// <param name="trustedSecrets">The raw HMAC secrets the receiver trusts.</param>
    /// <param name="trustedPublicKeys">The raw Ed25519 public keys the receiver trusts.</param>
    /// <param name="keyIdHeader">The advisory <c language="text">Cratis-Key-Id</c> header value; ignored.</param>
    /// <returns>True when any entry is a valid signature from a trusted key; otherwise false.</returns>
    /// <exception cref="InvalidWebhookMessageId">The message identifier contains a framing delimiter; the delivery must be rejected.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">The identifier cannot be encoded losslessly as UTF-8.</exception>
    internal static bool Verify(
        string messageId,
        long unixTimestamp,
        ReadOnlySpan<byte> payload,
        string? signatureHeader,
        IEnumerable<ReadOnlyMemory<byte>> trustedSecrets,
        IEnumerable<ReadOnlyMemory<byte>> trustedPublicKeys,
        string? keyIdHeader)
    {
        _ = keyIdHeader;

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var secrets = trustedSecrets.ToArray();
        var publicKeys = trustedPublicKeys.ToArray();
        if (secrets.Length == 0 && publicKeys.Length == 0)
        {
            return false;
        }

        var message = WebhookSigning.Frame(messageId, unixTimestamp, payload);
        var expected = new byte[secrets.Length][];
        for (var i = 0; i < secrets.Length; i++)
        {
            expected[i] = WebhookSigning.ComputeSignature(message, secrets[i].Span);
        }

        var valid = false;
        foreach (var entry in signatureHeader.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            valid |= EntryMatches(entry, message, expected, publicKeys);
        }

        return valid;
    }

    static bool EntryMatches(string entry, byte[] message, byte[][] expected, ReadOnlyMemory<byte>[] publicKeys)
    {
        var separator = entry.IndexOf(',');
        if (separator < 0)
        {
            return false;
        }

        var signature = entry[(separator + 1)..];
        return entry[..separator] switch
        {
            HmacVersion => HmacMatches(signature, expected),
            Ed25519Version => Ed25519Matches(signature, message, publicKeys),
            _ => false
        };
    }

    static bool HmacMatches(string signature, byte[][] expected)
    {
        Span<byte> decoded = stackalloc byte[32];
        if (!Convert.TryFromBase64String(signature, decoded, out var written) || written != decoded.Length)
        {
            return false;
        }

        var matched = false;
        foreach (var candidate in expected)
        {
            matched |= CryptographicOperations.FixedTimeEquals(decoded, candidate);
        }

        return matched;
    }

    static bool Ed25519Matches(string signature, byte[] message, ReadOnlyMemory<byte>[] publicKeys)
    {
        Span<byte> decoded = stackalloc byte[64];
        if (!Convert.TryFromBase64String(signature, decoded, out var written) || written != decoded.Length)
        {
            return false;
        }

        var matched = false;
        foreach (var publicKey in publicKeys)
        {
            matched |= WebhookEd25519Signing.VerifySignature(message, decoded, publicKey.Span);
        }

        return matched;
    }
}
