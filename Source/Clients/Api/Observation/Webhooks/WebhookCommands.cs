// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.Contracts.Observation.Webhooks;

namespace Cratis.Chronicle.Api.Observation.Webhooks;

/// <summary>
/// Represents the API for working with webhooks.
/// </summary>
[Route("/api/event-store/{eventStore}/observers/webhooks")]
public class WebhookCommands : ControllerBase
{
    readonly IWebhooks _webhooks;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookCommands"/> class.
    /// </summary>
    /// <param name="webhooks"><see cref="IWebhooks"/> for working with webhooks.</param>
    internal WebhookCommands(IWebhooks webhooks)
    {
        _webhooks = webhooks;
    }

    /// <summary>
    /// Register a webhook on an event store.
    /// </summary>
    /// <param name="eventStore">Name of the event store the observer is for.</param>
    /// <param name="command"><see cref="RegisterWebhook"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("/register")]
    public async Task<Guid> RegisterWebhook(
        [FromRoute] string eventStore,
        [FromBody] RegisterWebhook command)
    {
        var identifier = Guid.NewGuid();
        await _webhooks.Register(new()
        {
            EventStore = eventStore, Owner = Contracts.Observation.ObserverOwner.Client, Webhooks =
            [
                new()
                {
                    EventSequenceId = command.EventSequenceId,
                    EventTypes = command.EventTypes.Select(_ => _.ToContract()).ToList(),
                    Target = new Contracts.Observation.Webhooks.WebhookTarget()
                    {
                        Authentication = AuthenticationType.None,
                        Url = command.Target.Url
                    },
                    Identifier = identifier.ToString(),
                    IsActive = true,
                    IsReplayable = true
                }
            ]
        });

        return identifier;
    }
}
