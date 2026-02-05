// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Security;
using IWebhooksService = Cratis.Chronicle.Contracts.Observation.Webhooks.IWebhooks;

namespace Cratis.Chronicle.Api.Webhooks;

/// <summary>
/// Represents the API for working with webhook commands.
/// </summary>
[Route("/api/event-store/{eventStore}/webhooks")]
public class WebhookCommands : ControllerBase
{
    readonly IWebhooksService _webhooks;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookCommands"/> class.
    /// </summary>
    /// <param name="webhooks"><see cref="IWebhooksService"/> for working with webhooks.</param>
    internal WebhookCommands(IWebhooksService webhooks)
    {
        _webhooks = webhooks;
    }

    /// <summary>
    /// Create a new webhook.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="command">Command for creating the webhook.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("create")]
    public async Task CreateWebhook(
        [FromRoute] string eventStore,
        [FromBody] CreateWebhook command)
    {
        var webhookId = Guid.NewGuid().ToString();
        var headers = command.Headers.ToDictionary(h => h.Key, h => h.Value);

        var authorization = CreateAuthorization(command);

        var webhookTarget = new Contracts.Observation.Webhooks.WebhookTarget
        {
            Url = command.Url,
            Authorization = authorization,
            Headers = headers
        };

        await _webhooks.Register(new RegisterWebhook
        {
            EventStore = eventStore,
            Owner = Contracts.Observation.ObserverOwner.Client,
            Webhooks =
            [
                new Contracts.Observation.Webhooks.WebhookDefinition
                {
                    EventSequenceId = "event-log",
                    Identifier = webhookId,
                    EventTypes = command.EventTypes.Select(et => new Contracts.Events.EventType
                    {
                        Id = et.Key,
                        Generation = 1
                    }).ToList(),
                    Target = webhookTarget,
                    IsReplayable = command.IsReplayable,
                    IsActive = command.IsActive
                }
            ]
        });
    }

    static OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization>? CreateAuthorization(CreateWebhook command)
    {
        if (command.AuthorizationType.Equals("basic", StringComparison.OrdinalIgnoreCase))
        {
            return new(new BasicAuthorization
            {
                Username = command.BasicUsername ?? string.Empty,
                Password = command.BasicPassword ?? string.Empty
            });
        }

        if (command.AuthorizationType.Equals("bearer", StringComparison.OrdinalIgnoreCase))
        {
            return new(new BearerTokenAuthorization
            {
                Token = command.BearerToken ?? string.Empty
            });
        }

        if (command.AuthorizationType.Equals("oauth", StringComparison.OrdinalIgnoreCase))
        {
            return new(new OAuthAuthorization
            {
                Authority = command.OAuthAuthority ?? string.Empty,
                ClientId = command.OAuthClientId ?? string.Empty,
                ClientSecret = command.OAuthClientSecret ?? string.Empty
            });
        }

        return null;
    }
}
