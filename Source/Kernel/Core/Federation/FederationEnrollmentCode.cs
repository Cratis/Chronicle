// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The one string an operator hands to a customer to enroll a kernel: the public endpoint, the fingerprint of the authority
/// root and the token secret. Treat the formatted string as a password. It is single-use and expires.
/// </summary>
/// <param name="Endpoint">The public https endpoint of the federation listener.</param>
/// <param name="AuthorityFingerprint">The SHA-256 fingerprint of the authority certificate, as lowercase hex.</param>
/// <param name="EnrollmentId">The enrollment id, which is public.</param>
/// <param name="Secret">The token secret.</param>
public record FederationEnrollmentCode(string Endpoint, string AuthorityFingerprint, string EnrollmentId, string Secret)
{
    const string Prefix = "cfe1.";

    /// <summary>
    /// Parse a formatted code.
    /// </summary>
    /// <param name="value">The formatted code.</param>
    /// <param name="code">The parsed code, when the value is valid.</param>
    /// <returns>True when the value is a valid code.</returns>
    public static bool TryParse(string? value, out FederationEnrollmentCode code)
    {
        code = null!;
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed) || !trimmed.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var bytes = FromBase64Url(trimmed[Prefix.Length..]);
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            var endpoint = root.GetProperty("e").GetString();
            var fingerprint = root.GetProperty("f").GetString();
            var id = root.GetProperty("i").GetString();
            var secret = root.GetProperty("s").GetString();
            if (string.IsNullOrEmpty(endpoint)
                || string.IsNullOrEmpty(fingerprint)
                || string.IsNullOrEmpty(id)
                || string.IsNullOrEmpty(secret)
                || !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            code = new(endpoint, fingerprint.ToLowerInvariant(), id, secret);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or KeyNotFoundException or InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Format the code as the single string handed to the customer.
    /// </summary>
    /// <returns>The formatted code.</returns>
    public string Format()
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string>
        {
            ["e"] = Endpoint,
            ["f"] = AuthorityFingerprint,
            ["i"] = EnrollmentId,
            ["s"] = Secret
        });
        return Prefix + Convert.ToBase64String(json).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Gets a description that never contains the secret.
    /// </summary>
    /// <returns>The description.</returns>
    public override string ToString() => $"FederationEnrollmentCode {{ Endpoint = {Endpoint}, EnrollmentId = {EnrollmentId}, Secret = *** }}";

    static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
