// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the command for removing webhooks from an event store.
/// </summary>
/// <param name="EventStore">The event store the webhooks belong to.</param>
/// <param name="Webhooks">The identifiers of the webhooks to remove.</param>
[Command]
[BelongsTo(WellKnownServices.Webhooks)]
public record RemoveWebhooks(EventStoreName EventStore, IEnumerable<WebhookId> Webhooks)
{
    /// <summary>
    /// Handles the command by appending a removal for each webhook.
    /// </summary>
    /// <param name="registrar">The <see cref="WebhookRegistrar"/> that appends them.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(WebhookRegistrar registrar) => registrar.Remove(EventStore, Webhooks.Select(_ => _.Value));
}
