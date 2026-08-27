// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the command for removing a single webhook.
/// </summary>
/// <param name="EventStore">The event store the webhook belongs to.</param>
/// <param name="WebhookId">The identifier of the webhook to remove.</param>
[Command]
[BelongsTo(WellKnownServices.Webhooks)]
public record RemoveWebhook(EventStoreName EventStore, WebhookId WebhookId)
{
    /// <summary>
    /// Handles the command by appending the removal.
    /// </summary>
    /// <param name="registrar">The <see cref="WebhookRegistrar"/> that appends it.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(WebhookRegistrar registrar) => registrar.Remove(EventStore, [WebhookId]);
}
