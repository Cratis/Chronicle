// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents a webhook signing key, with its private material kept only as ciphertext.
/// </summary>
/// <param name="KeyId">The unique identifier of the key.</param>
/// <param name="Algorithm">The algorithm the key is used with.</param>
/// <param name="Status">The lifecycle status of the key.</param>
/// <param name="PublicKey">The whpk_ encoded public key, empty for symmetric keys.</param>
/// <param name="EncryptedSecret">The encrypted private material, empty once the key is retired.</param>
/// <param name="Webhook">The webhook the key is scoped to, or null for a store-wide key.</param>
/// <param name="CreatedAt">When the key was created.</param>
/// <param name="OverlapEndsAt">When the overlap window of a superseded key ends.</param>
/// <param name="RetiredAt">When the key was retired.</param>
public record WebhookSigningKey(
    WebhookSigningKeyId KeyId,
    WebhookSigningAlgorithm Algorithm,
    WebhookSigningKeyStatus Status,
    string PublicKey,
    EncryptedWebhookSigningSecret EncryptedSecret,
    WebhookId? Webhook,
    DateTimeOffset CreatedAt,
    DateTimeOffset? OverlapEndsAt,
    DateTimeOffset? RetiredAt)
{
    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(WebhookSigningKey)} {{ KeyId = {KeyId.Value}, Algorithm = {Algorithm}, Status = {Status}, OverlapEndsAt = {OverlapEndsAt}, RetiredAt = {RetiredAt} }}";
}
