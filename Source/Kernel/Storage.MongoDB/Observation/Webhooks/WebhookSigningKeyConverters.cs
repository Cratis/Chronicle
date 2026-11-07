// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks;

/// <summary>
/// Provides extension methods for converting between Kernel and MongoDB <see cref="WebhookSigningKey"/> representations.
/// </summary>
public static class WebhookSigningKeyConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Observation.Webhooks.WebhookSigningKey"/> to a MongoDB <see cref="WebhookSigningKey"/>.
    /// </summary>
    /// <param name="key">The Kernel signing key.</param>
    /// <returns>The MongoDB signing key.</returns>
    public static WebhookSigningKey ToMongoDB(this Concepts.Observation.Webhooks.WebhookSigningKey key) =>
        new()
        {
            Id = key.KeyId,
            Algorithm = key.Algorithm,
            Status = key.Status,
            PublicKey = key.PublicKey,
            EncryptedSecret = key.EncryptedSecret.Value,
            Webhook = key.Webhook?.Value,
            CreatedAt = key.CreatedAt,
            OverlapEndsAt = key.OverlapEndsAt,
            RetiredAt = key.RetiredAt
        };

    /// <summary>
    /// Converts a MongoDB <see cref="WebhookSigningKey"/> to a Kernel <see cref="Concepts.Observation.Webhooks.WebhookSigningKey"/>.
    /// </summary>
    /// <param name="key">The MongoDB signing key.</param>
    /// <returns>The Kernel signing key.</returns>
    public static Concepts.Observation.Webhooks.WebhookSigningKey ToKernel(this WebhookSigningKey key) =>
        new(
            key.Id,
            key.Algorithm,
            key.Status,
            key.PublicKey,
            new EncryptedWebhookSigningSecret(key.EncryptedSecret),
            key.Webhook is null ? null : new WebhookId(key.Webhook),
            key.CreatedAt,
            key.OverlapEndsAt,
            key.RetiredAt);
}
