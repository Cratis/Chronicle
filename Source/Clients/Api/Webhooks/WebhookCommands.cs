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
    /// Add a new webhook.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="command">Command for adding the webhook.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("add")]
    public async Task AddWebHook(
        [FromRoute] string eventStore,
        [FromBody] AddWebhook command)
    {
        var headers = command.Headers.ToDictionary(h => h.Key, h => h.Value);

        var authorization = CreateAuthorization(command);

        var webhookTarget = new WebhookTarget
        {
            Url = command.Url,
            Authorization = authorization,
            Headers = headers
        };

        await _webhooks.Add(new Contracts.Observation.Webhooks.AddWebhooks
        {
            EventStore = eventStore,
            Owner = Contracts.Observation.ObserverOwner.Client,
            Webhooks =
            [
                new Contracts.Observation.Webhooks.WebhookDefinition
                {
                    EventSequenceId = command.EventSequenceId,
                    Identifier = command.Name,
                    EventTypes = command.EventTypes.Select(et => new Contracts.Events.EventType
                    {
                        Id = et.Id,
                        Generation = et.Generation
                    }).ToList(),
                    Target = webhookTarget,
                    IsReplayable = command.IsReplayable,
                    IsActive = command.IsActive
                }
            ]
        });
    }

    /// <summary>
    /// Remove a webhook.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="command">Command for removing the webhook.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("remove")]
    public async Task RemoveWebHook(
        [FromRoute] string eventStore,
        [FromBody] RemoveWebhook command)
    {
        await _webhooks.Remove(new RemoveWebhooks
        {
            EventStore = eventStore,
            Webhooks = [command.WebhookId]
        });
    }

    /// <summary>
    /// Test OAuth authorization.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <param name="command">Command for testing OAuth authorization.</param>
    /// <returns>Result of the OAuth authorization test.</returns>
    [HttpPost("test-oauth")]
    public async Task<TestOAuthAuthorizationResult> TestOAuthAuthorization(
        [FromRoute] string eventStore,
        [FromBody] TestOAuthAuthorization command)
    {
        var result = await _webhooks.TestOAuthAuthorization(new Contracts.Observation.Webhooks.TestOAuthAuthorizationRequest
        {
            Authority = command.OAuthAuthority,
            ClientId = command.OAuthClientId,
            ClientSecret = command.OAuthClientSecret
        });

        return new TestOAuthAuthorizationResult
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }

    static OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization>? CreateAuthorization(AddWebhook command)
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
