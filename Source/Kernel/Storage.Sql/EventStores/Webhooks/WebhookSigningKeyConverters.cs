// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks;

/// <summary>
/// Provides extension methods for converting between Kernel and SQL <see cref="WebhookSigningKey"/> representations.
/// </summary>
public static class WebhookSigningKeyConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Observation.Webhooks.WebhookSigningKey"/> to a SQL <see cref="WebhookSigningKey"/>.
    /// </summary>
    /// <param name="key">The Kernel signing key.</param>
    /// <returns>The SQL signing key.</returns>
    public static WebhookSigningKey ToSql(this Concepts.Observation.Webhooks.WebhookSigningKey key) =>
        new()
        {
            KeyId = key.KeyId.Value,
            Algorithm = (int)key.Algorithm,
            Status = (int)key.Status,
            PublicKey = key.PublicKey,
            EncryptedSecret = key.EncryptedSecret.Value,
            Webhook = key.Webhook?.Value,
            CreatedAt = key.CreatedAt,
            OverlapEndsAt = key.OverlapEndsAt,
            RetiredAt = key.RetiredAt
        };

    /// <summary>
    /// Converts a SQL <see cref="WebhookSigningKey"/> to a Kernel <see cref="Concepts.Observation.Webhooks.WebhookSigningKey"/>.
    /// </summary>
    /// <param name="key">The SQL signing key.</param>
    /// <returns>The Kernel signing key.</returns>
    public static Concepts.Observation.Webhooks.WebhookSigningKey ToKernel(this WebhookSigningKey key) =>
        new(
            new WebhookSigningKeyId(key.KeyId),
            (WebhookSigningAlgorithm)key.Algorithm,
            (WebhookSigningKeyStatus)key.Status,
            key.PublicKey,
            new EncryptedWebhookSigningSecret(key.EncryptedSecret),
            key.Webhook is null ? null : new WebhookId(key.Webhook),
            key.CreatedAt,
            key.OverlapEndsAt,
            key.RetiredAt);
}
