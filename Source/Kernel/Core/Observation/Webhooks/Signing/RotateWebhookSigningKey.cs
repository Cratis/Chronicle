// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Rotates an event store's Ed25519 delivery signing key without returning private material.
/// </summary>
/// <param name="EventStore">The event store whose signing key to rotate.</param>
/// <param name="OverlapSeconds">The overlap in seconds, or null for the 24-hour default. Zero retires the previous key immediately.</param>
[Command]
[BelongsTo(WellKnownServices.Webhooks)]
public record RotateWebhookSigningKey(EventStoreName EventStore, double? OverlapSeconds = null)
{
    /// <summary>
    /// Rotates the key inside the kernel boundary.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>A task with no key material.</returns>
    public Task Handle(IGrainFactory grainFactory) => WebhookSigningKeyOperations.Rotate(EventStore, OverlapSeconds, grainFactory);
}
