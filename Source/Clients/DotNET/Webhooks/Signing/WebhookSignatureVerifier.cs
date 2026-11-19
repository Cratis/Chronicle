// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Verifies Standard Webhooks <c language="text">v1a</c> (Ed25519, RFC 8032) signatures against a set of configured public keys.
/// </summary>
/// <remarks>
/// A delivery is valid when any <c language="text">v1a</c> signature in the header verifies under any configured key, which is
/// how key rotation overlap works: the kernel emits one signature per active or overlapping key. Signatures of other versions
/// are ignored. A key that is no longer listed is rejected only because it is no longer configured; keys are not expired here.
/// Widening the tolerance weakens replay protection.
/// </remarks>
public class WebhookSignatureVerifier : IWebhookSignatureVerifier
{
    const string PublicKeyPrefix = "whpk_";
    const string SupportedVersionPrefix = "v1a,";
    const int MaxSignatureTokens = 16;
    const int MaxSignatureHeaderLength = 4096;
    const int SignatureLength = 64;
    const int PublicKeyLength = 32;

    static readonly TimeSpan _defaultTolerance = TimeSpan.FromMinutes(5);
    static readonly UTF8Encoding _utf8 = new(false, true);

    readonly (string KeyId, byte[] PublicKey)[] _keys;
    readonly TimeSpan _tolerance;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookSignatureVerifier"/> class.
    /// </summary>
    /// <param name="keys">The public keys to trust, typically the ones the event store publishes.</param>
    /// <param name="tolerance">How far the timestamp may be from the current time, in either direction. Defaults to five minutes.</param>
    /// <param name="timeProvider">The time source. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentException">No keys were given, or a key is malformed.</exception>
    public WebhookSignatureVerifier(IEnumerable<WebhookSigningPublicKey> keys, TimeSpan? tolerance = null, TimeProvider? timeProvider = null)
        : this(keys.Select(_ => (_.KeyId, _.PublicKey)), tolerance, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookSignatureVerifier"/> class.
    /// </summary>
    /// <param name="publicKeys">The <c language="text">whpk_</c> prefixed public keys to trust. A key is reported as its own identifier.</param>
    /// <param name="tolerance">How far the timestamp may be from the current time, in either direction. Defaults to five minutes.</param>
    /// <param name="timeProvider">The time source. Defaults to <see cref="TimeProvider.System"/>.</param>
    /// <exception cref="ArgumentException">No keys were given, or a key is malformed.</exception>
    public WebhookSignatureVerifier(IEnumerable<string> publicKeys, TimeSpan? tolerance = null, TimeProvider? timeProvider = null)
        : this(publicKeys.Select(_ => (_, _)), tolerance, timeProvider)
    {
    }

    WebhookSignatureVerifier(IEnumerable<(string KeyId, string PublicKey)> keys, TimeSpan? tolerance, TimeProvider? timeProvider)
    {
        _keys = [.. keys.Select(_ => (_.KeyId, Decode(_.PublicKey)))];
        if (_keys.Length == 0)
        {
            throw new ArgumentException("At least one public key is required.", nameof(keys));
        }

        _tolerance = tolerance ?? _defaultTolerance;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc/>
    public WebhookSignatureVerificationResult Verify(string? webhookId, string? webhookTimestamp, string? webhookSignature, ReadOnlySpan<byte> payload)
    {
        var failure = ValidateHeaders(webhookId, webhookTimestamp, webhookSignature);
        if (failure != WebhookSignatureFailure.None)
        {
            return Fail(failure);
        }

        (failure, var signatures) = ParseSignatures(webhookSignature!);
        if (failure != WebhookSignatureFailure.None)
        {
            return Fail(failure);
        }

        if (!TryFrame(webhookId!, webhookTimestamp!, payload, out var message))
        {
            return Fail(WebhookSignatureFailure.InvalidId);
        }

        foreach (var (keyId, publicKey) in _keys)
        {
            if (signatures.Exists(signature => VerifySignature(message, signature, publicKey)))
            {
                return new(WebhookSignatureFailure.None, keyId);
            }
        }

        return Fail(WebhookSignatureFailure.SignatureMismatch);
    }

    static WebhookSignatureVerificationResult Fail(WebhookSignatureFailure failure) => new(failure);

    static byte[] Decode(string publicKey)
    {
        if (publicKey?.StartsWith(PublicKeyPrefix, StringComparison.Ordinal) != true)
        {
            throw new ArgumentException($"A public key must start with '{PublicKeyPrefix}'.", nameof(publicKey));
        }

        var buffer = new byte[PublicKeyLength];
        if (!Convert.TryFromBase64String(publicKey[PublicKeyPrefix.Length..], buffer, out var written) || written != PublicKeyLength)
        {
            throw new ArgumentException($"A public key must be the Base64 encoding of exactly {PublicKeyLength} bytes.", nameof(publicKey));
        }

        return buffer;
    }

    static bool TryParseTimestamp(string value, out long timestamp) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out timestamp) &&
        timestamp.ToString(CultureInfo.InvariantCulture) == value;

    static (WebhookSignatureFailure Failure, List<byte[]> Signatures) ParseSignatures(string header)
    {
        var tokens = header.Length > MaxSignatureHeaderLength ? [] : header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (header.Length > MaxSignatureHeaderLength || tokens.Length > MaxSignatureTokens)
        {
            return (WebhookSignatureFailure.SignatureMismatch, []);
        }

        var supported = tokens.Where(token => token.StartsWith(SupportedVersionPrefix, StringComparison.Ordinal)).ToList();
        if (supported.Count == 0)
        {
            return (WebhookSignatureFailure.NoSupportedSignature, []);
        }

        var signatures = supported.Select(DecodeSignature).OfType<byte[]>().ToList();
        return (signatures.Count == 0 ? WebhookSignatureFailure.SignatureMismatch : WebhookSignatureFailure.None, signatures);
    }

    static byte[]? DecodeSignature(string token)
    {
        var buffer = new byte[SignatureLength];
        return Convert.TryFromBase64String(token[SupportedVersionPrefix.Length..], buffer, out var written) && written == SignatureLength ? buffer : null;
    }

    static bool TryFrame(string id, string timestamp, ReadOnlySpan<byte> payload, out byte[] message)
    {
        try
        {
            var prefix = _utf8.GetBytes($"{id}.{timestamp}.");
            message = new byte[prefix.Length + payload.Length];
            prefix.CopyTo(message, 0);
            payload.CopyTo(message.AsSpan(prefix.Length));
            return true;
        }
        catch (EncoderFallbackException)
        {
            message = [];
            return false;
        }
    }

    static bool VerifySignature(byte[] message, byte[] signature, byte[] publicKey)
    {
        try
        {
            var verifier = new Ed25519Signer();
            verifier.Init(false, new Ed25519PublicKeyParameters(publicKey));
            verifier.BlockUpdate(message, 0, message.Length);
            return verifier.VerifySignature(signature);
        }
        catch (ArgumentException)
        {
            // An invalid point encoding is untrusted input, not an error to propagate.
            return false;
        }
    }

    static bool AnyMissing(
        [NotNullWhen(false)] string? id,
        [NotNullWhen(false)] string? timestamp,
        [NotNullWhen(false)] string? signature) =>
        string.IsNullOrEmpty(id) || string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(signature);

    WebhookSignatureFailure ValidateHeaders(string? id, string? timestamp, string? signature)
    {
        if (AnyMissing(id, timestamp, signature))
        {
            return WebhookSignatureFailure.MissingHeader;
        }

        if (id.Contains('.'))
        {
            return WebhookSignatureFailure.InvalidId;
        }

        if (!TryParseTimestamp(timestamp, out var unixSeconds))
        {
            return WebhookSignatureFailure.InvalidTimestamp;
        }

        return IsOutsideTolerance(unixSeconds)
            ? WebhookSignatureFailure.TimestampOutsideTolerance
            : WebhookSignatureFailure.None;
    }

    bool IsOutsideTolerance(long unixSeconds) =>
        Math.Abs(_timeProvider.GetUtcNow().ToUnixTimeSeconds() - unixSeconds) > _tolerance.TotalSeconds;
}
