// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation.Webhooks;
using IWebhooksService = Cratis.Chronicle.Contracts.Observation.Webhooks.IWebhooks;

namespace Cratis.Chronicle.Api.Webhooks;

/// <summary>
/// Represents the API for working with webhook queries.
/// </summary>
[Route("/api/event-store/{eventStore}/webhooks")]
public class WebhookQueries : ControllerBase
{
    readonly IWebhooksService _webhooks;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookQueries"/> class.
    /// </summary>
    /// <param name="webhooks"><see cref="IWebhooksService"/> for working with webhooks.</param>
    internal WebhookQueries(IWebhooksService webhooks)
    {
        _webhooks = webhooks;
    }

    /// <summary>
    /// Get all webhooks for an event store.
    /// </summary>
    /// <param name="eventStore">Name of the event store.</param>
    /// <returns>Collection of webhook definitions.</returns>
    [HttpGet]
    public async Task<IEnumerable<WebhookDefinition>> GetWebhooks([FromRoute] string eventStore)
    {
        var webhooks = await _webhooks.GetWebhooks(new GetWebhooksRequest { EventStore = eventStore });
        return webhooks.Select(w => new WebhookDefinition
        {
            Id = w.Identifier,
            Name = w.Identifier,
            Url = w.Target.Url,
            EventTypes = w.EventTypes.ToDictionary(et => et.Id, et => string.Empty),
            AuthorizationType = GetAuthorizationType(w.Target),
            Headers = w.Target.Headers,
            IsReplayable = w.IsReplayable,
            IsActive = w.IsActive
        });
    }

    static string GetAuthorizationType(Contracts.Observation.Webhooks.WebhookTarget target)
    {
        if (target.Authorization is null) return "None";
        
        if (target.Authorization.Value0 is not null) return "Basic";
        if (target.Authorization.Value1 is not null) return "Bearer";
        if (target.Authorization.Value2 is not null) return "OAuth";
        
        return "None";
    }
}
