// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the command for registering webhooks with an event store.
/// </summary>
/// <param name="EventStore">The event store the webhooks belong to.</param>
/// <param name="Webhooks">The webhook definitions to register.</param>
[Command]
[BelongsTo(WellKnownServices.Webhooks)]
public record AddWebhooks(
    string EventStore,
    IEnumerable<Contracts.Observation.Webhooks.WebhookDefinition> Webhooks)
{
    /// <summary>
    /// Handles the command by appending the events each changed definition implies.
    /// </summary>
    /// <param name="registrar">The <see cref="WebhookRegistrar"/> that decides what changed.</param>
    /// <returns>Awaitable task.</returns>
    internal Task Handle(WebhookRegistrar registrar) => registrar.Add(EventStore, Webhooks);
}
