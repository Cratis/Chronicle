// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Defines a verifier for Standard Webhooks <c language="text">v1a</c> (Ed25519) signatures on webhook deliveries.
/// </summary>
public interface IWebhookSignatureVerifier
{
    /// <summary>
    /// Verifies a delivery. Never throws for untrusted input; every rejection is reported as a <see cref="WebhookSignatureFailure"/>.
    /// </summary>
    /// <param name="webhookId">The value of the <see cref="WebhookSigningHeaders.Id"/> header.</param>
    /// <param name="webhookTimestamp">The value of the <see cref="WebhookSigningHeaders.Timestamp"/> header.</param>
    /// <param name="webhookSignature">The value of the <see cref="WebhookSigningHeaders.Signature"/> header.</param>
    /// <param name="payload">The exact request body bytes as received, not re-serialized.</param>
    /// <returns>The result of the verification.</returns>
    /// <remarks>
    /// The <see cref="WebhookSigningHeaders.KeyId"/> header is advisory and deliberately not an input: a sender must
    /// not be able to choose which configured key is trusted.
    /// </remarks>
    WebhookSignatureVerificationResult Verify(string? webhookId, string? webhookTimestamp, string? webhookSignature, ReadOnlySpan<byte> payload);
}
