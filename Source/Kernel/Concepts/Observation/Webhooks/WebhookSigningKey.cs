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
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Observation.Webhooks.WebhookSigningKey")]
public record WebhookSigningKey(
    [Id(0)] WebhookSigningKeyId KeyId,
    [Id(1)] WebhookSigningAlgorithm Algorithm,
    [Id(2)] WebhookSigningKeyStatus Status,
    [Id(3)] string PublicKey,
    [Id(4)] EncryptedWebhookSigningSecret EncryptedSecret,
    [Id(5)] WebhookId? Webhook,
    [Id(6)] DateTimeOffset CreatedAt,
    [Id(7)] DateTimeOffset? OverlapEndsAt,
    [Id(8)] DateTimeOffset? RetiredAt)
{
    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(WebhookSigningKey)} {{ KeyId = {KeyId.Value}, Algorithm = {Algorithm}, Status = {Status}, OverlapEndsAt = {OverlapEndsAt}, RetiredAt = {RetiredAt} }}";
}
