// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Represents the event for a webhook signing key that has been generated.
/// </summary>
/// <remarks>
/// The event carries no private material, not even ciphertext. The encrypted key lives only in storage.
/// </remarks>
/// <param name="KeyId">The identifier of the key.</param>
/// <param name="Algorithm">The algorithm of the key.</param>
/// <param name="PublicKey">The encoded public key, empty for symmetric keys.</param>
/// <param name="Webhook">The webhook the key is scoped to, or null for a store-wide key.</param>
[EventType, AllEventStores]
public record WebhookSigningKeyGenerated(
    WebhookSigningKeyId KeyId,
    WebhookSigningAlgorithm Algorithm,
    string PublicKey,
    WebhookId? Webhook);
