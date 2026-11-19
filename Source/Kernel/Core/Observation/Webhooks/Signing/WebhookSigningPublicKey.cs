// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Public metadata for an event store's Ed25519 delivery signing key.
/// </summary>
/// <param name="KeyId">The advisory key identifier.</param>
/// <param name="Algorithm">The signing algorithm.</param>
/// <param name="PublicKey">The whpk_ encoded verification key.</param>
/// <param name="Status">The key's active or overlap status.</param>
/// <param name="CreatedAt">When the key was created.</param>
/// <param name="OverlapEndsAt">When an overlap key stops being valid.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Webhooks)]
public record WebhookSigningPublicKey(
    string KeyId,
    WebhookSigningAlgorithm Algorithm,
    string PublicKey,
    WebhookSigningKeyStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? OverlapEndsAt)
{
    /// <summary>
    /// Gets the active and still-valid overlap public keys for an event store.
    /// </summary>
    /// <param name="eventStore">The event store whose keys to get.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>Public verification metadata only.</returns>
    public static Task<IEnumerable<WebhookSigningPublicKey>> GetWebhookSigningKeys(EventStoreName eventStore, IGrainFactory grainFactory) =>
        WebhookSigningKeyOperations.GetPublicKeys(eventStore, grainFactory);
}
