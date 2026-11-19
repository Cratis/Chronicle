// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Projects signing state into public-only verification metadata.
/// </summary>
internal static class WebhookSigningPublicKeyConverters
{
    internal static IEnumerable<WebhookSigningPublicKey> ToPublicKeys(IEnumerable<WebhookSigningKey> keys, DateTimeOffset now) => keys
        .Where(key => key.Webhook is null && key.Algorithm == WebhookSigningAlgorithm.Ed25519 &&
            (key.Status == WebhookSigningKeyStatus.Active || (key.Status == WebhookSigningKeyStatus.Overlap && key.OverlapEndsAt > now)))
        .OrderBy(key => key.Status)
        .ThenBy(key => key.OverlapEndsAt)
        .ThenBy(key => key.KeyId.Value, StringComparer.Ordinal)
        .Select(ToPublicKey)
        .ToArray();

    static WebhookSigningPublicKey ToPublicKey(WebhookSigningKey key)
    {
        if (!WebhookSigningKeys.TryParsePublicKey(key.PublicKey, out _)) throw new WebhookSigningUnavailable();
        return new(key.KeyId.Value, key.Algorithm, key.PublicKey, key.Status, key.CreatedAt, key.OverlapEndsAt);
    }
}
