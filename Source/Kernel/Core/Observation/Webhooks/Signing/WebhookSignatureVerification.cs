// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Verifies a Standard Webhooks <c language="text">webhook-signature</c> header against a set of trusted secrets.
/// </summary>
internal static class WebhookSignatureVerification
{
    const string HmacVersion = "v1";

    /// <summary>
    /// Verifies that at least one signature in the header was produced by a trusted secret.
    /// </summary>
    /// <remarks>
    /// The <c language="text">Cratis-Key-Id</c> header is advisory: <paramref name="keyIdHeader"/> is accepted only to make that
    /// contract explicit and never influences the result. The caller owns the timestamp tolerance policy.
    /// Only the <c language="text">v1</c> (HMAC-SHA256) version is supported; entries of any other version are ignored, as are
    /// entries that are not valid base64.
    /// </remarks>
    /// <param name="messageId">The <c language="text">webhook-id</c> value.</param>
    /// <param name="unixTimestamp">The <c language="text">webhook-timestamp</c> value in Unix seconds.</param>
    /// <param name="payload">The exact received payload bytes.</param>
    /// <param name="signatureHeader">The <c language="text">webhook-signature</c> header value: space-separated <c language="text">version,signature</c> entries.</param>
    /// <param name="trustedSecrets">The raw secrets the receiver trusts.</param>
    /// <param name="keyIdHeader">The advisory <c language="text">Cratis-Key-Id</c> header value; ignored.</param>
    /// <returns>True when any entry is a valid signature from a trusted secret; otherwise false.</returns>
    internal static bool Verify(
        string messageId,
        long unixTimestamp,
        ReadOnlySpan<byte> payload,
        string? signatureHeader,
        IEnumerable<ReadOnlyMemory<byte>> trustedSecrets,
        string? keyIdHeader)
    {
        _ = keyIdHeader;

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        var secrets = trustedSecrets.ToArray();
        if (secrets.Length == 0)
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
            valid |= EntryMatches(entry, expected);
        }

        return valid;
    }

    static bool EntryMatches(string entry, byte[][] expected)
    {
        var separator = entry.IndexOf(',');
        if (separator < 0 || entry[..separator] != HmacVersion)
        {
            return false;
        }

        Span<byte> decoded = stackalloc byte[32];
        if (!Convert.TryFromBase64String(entry[(separator + 1)..], decoded, out var written) || written != decoded.Length)
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
}
