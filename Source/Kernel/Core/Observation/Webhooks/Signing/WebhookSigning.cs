// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Signs caller-owned bytes using the Standard Webhooks HMAC-SHA256 scheme.
/// </summary>
internal static class WebhookSigning
{
    static readonly Encoding _utf8 = new UTF8Encoding(false, true);

    /// <summary>
    /// Frames the message identifier, attempt time, and exact payload bytes and signs them.
    /// </summary>
    /// <param name="messageId">The caller-owned identifier, without framing delimiters.</param>
    /// <param name="unixTimestamp">The caller-owned attempt time in Unix seconds.</param>
    /// <param name="payload">The exact bytes to be delivered, without serialization or text conversion.</param>
    /// <param name="secret">The raw HMAC secret, without encoded key prefixes.</param>
    /// <returns>A single v1 signature suitable for the webhook-signature header.</returns>
    /// <exception cref="InvalidWebhookMessageId">The identifier contains a framing delimiter.</exception>
    /// <exception cref="EncoderFallbackException">The identifier cannot be encoded losslessly as UTF-8.</exception>
    internal static string Sign(string messageId, long unixTimestamp, ReadOnlySpan<byte> payload, ReadOnlySpan<byte> secret)
    {
        // Standard Webhooks 1.0.0, "Signature scheme": identifiers must not contain '.'.
        if (messageId.Contains('.'))
        {
            throw new InvalidWebhookMessageId();
        }

        var prefix = _utf8.GetBytes($"{messageId}.{unixTimestamp.ToString(CultureInfo.InvariantCulture)}.");
        var message = new byte[checked(prefix.Length + payload.Length)];
        prefix.CopyTo(message, 0);
        payload.CopyTo(message.AsSpan(prefix.Length));
        return $"v1,{Convert.ToBase64String(ComputeSignature(message, secret))}";
    }

    /// <summary>
    /// Computes the primitive HMAC independently of protocol framing or header formatting.
    /// </summary>
    /// <param name="message">The bytes to authenticate.</param>
    /// <param name="secret">The raw HMAC secret.</param>
    /// <returns>The SHA-256 HMAC bytes.</returns>
    internal static byte[] ComputeSignature(ReadOnlySpan<byte> message, ReadOnlySpan<byte> secret) => HMACSHA256.HashData(secret, message);
}
