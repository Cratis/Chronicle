// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Rotates an event store's Ed25519 delivery signing key without returning private material.
/// </summary>
/// <param name="EventStore">The event store whose signing key to rotate.</param>
/// <param name="UseSpecifiedOverlap">Whether to use OverlapSeconds instead of the 24-hour default.</param>
/// <param name="OverlapSeconds">The overlap in seconds when UseSpecifiedOverlap is true. Zero retires the previous key immediately.</param>
[Command]
[Cratis.Arc.Authorization.Authorize(Policy = Security.WellKnownAuthorizationPolicies.Administrator)]
[BelongsTo(WellKnownServices.Webhooks)]
public record RotateWebhookSigningKey(EventStoreName EventStore, bool UseSpecifiedOverlap = false, double OverlapSeconds = 0)
{
    /// <summary>
    /// Rotates the key inside the kernel boundary.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>A task with no key material.</returns>
    public Task Handle(IGrainFactory grainFactory) => WebhookSigningKeyOperations.Rotate(EventStore, UseSpecifiedOverlap, OverlapSeconds, grainFactory);
}
