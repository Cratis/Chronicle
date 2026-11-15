// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.InMemory.Observation.Webhooks.for_WebhookSigningKeysStorage.given;

public static class signing_keys
{
    public static WebhookSigningKey Active(string id = "active") => new(id, WebhookSigningAlgorithm.Ed25519, WebhookSigningKeyStatus.Active, "whpk_a", "enc:whsk_a", null, DateTimeOffset.FromUnixTimeSeconds(100), null, null);

    public static WebhookSigningKey OverlapForWebhook(string id = "overlap") => new(id, WebhookSigningAlgorithm.HmacSha256, WebhookSigningKeyStatus.Overlap, string.Empty, "enc:whsec_b", "orders", DateTimeOffset.FromUnixTimeSeconds(110), DateTimeOffset.FromUnixTimeSeconds(200), null);

    public static WebhookSigningKey Retired(string id = "retired") => new(id, WebhookSigningAlgorithm.Ed25519, WebhookSigningKeyStatus.Retired, "whpk_c", EncryptedWebhookSigningSecret.Empty, null, DateTimeOffset.FromUnixTimeSeconds(120), DateTimeOffset.FromUnixTimeSeconds(200), DateTimeOffset.FromUnixTimeSeconds(300));
}
