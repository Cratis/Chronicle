// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Extension methods for reasoning about the delivery format of a <see cref="WebhookDefinition"/>.
/// </summary>
/// <remarks>
/// This is the only place in the kernel where the delivery format is compared. The 0020 amendment states that
/// HTTP delivery v1 delivers live events only: an observer that serves a delivery envelope is not replayable.
/// </remarks>
public static class WebhookDefinitionDeliveryExtensions
{
    /// <summary>
    /// Gets whether the webhook is delivered in the envelope format.
    /// </summary>
    /// <param name="definition">The <see cref="WebhookDefinition"/>.</param>
    /// <returns>True if the webhook uses <see cref="WebhookDeliveryFormat.EnvelopeV1"/>, false otherwise.</returns>
    public static bool UsesDeliveryEnvelope(this WebhookDefinition definition) =>
        definition.DeliveryFormat == WebhookDeliveryFormat.EnvelopeV1;

    /// <summary>
    /// Gets whether the webhook is live-only, meaning no replayed events are ever delivered to it.
    /// </summary>
    /// <param name="definition">The <see cref="WebhookDefinition"/>.</param>
    /// <returns>True if replays must never reach the target, false otherwise.</returns>
    public static bool IsLiveOnly(this WebhookDefinition definition) => definition.UsesDeliveryEnvelope();
}
