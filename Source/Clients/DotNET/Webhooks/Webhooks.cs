// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Webhooks;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhooks"/>.
/// </summary>
/// <param name="eventTypes">The <see cref="IEventTypes"/>.</param>
/// <param name="eventStore">The <see cref="IEventStore"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public class Webhooks(IEventTypes eventTypes, IEventStore eventStore, ILogger<Webhooks> logger) : IWebhooks
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public async Task Register(WebhookId webhookId, WebhookTargetUrl targetUrl, Action<IWebhookDefinitionBuilder> configure)
    {
        var definitionBuilder = new WebhookDefinitionBuilder(eventTypes);
        configure(definitionBuilder);
        var definition = definitionBuilder.Build(webhookId, targetUrl);
        logger.RegisterWebhook(
            definition.Identifier,
            definition.EventSequenceId);

        var request = new AddWebhooks
        {
            EventStore = eventStore.Name,
            Owner = ObserverOwner.Client,
            Webhooks = [definition.ToContract()]
        };

        await _servicesAccessor.Services.Webhooks.Add(request);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WebhookDefinition>> GetAll()
    {
        var request = new GetWebhooksRequest { EventStore = eventStore.Name };
        var webhooks = await _servicesAccessor.Services.Webhooks.GetWebhooks(request);

        return webhooks.Select(w => new WebhookDefinition(
            new WebhookId(w.Identifier),
            w.EventTypes.Select(et => new Events.EventType(new Events.EventTypeId(et.Id), new Events.EventTypeGeneration((uint)et.Generation))),
            new WebhookTarget(
                new WebhookTargetUrl(w.Target.Url),
                OneOf.OneOf<BasicAuthorization, BearerTokenAuthorization, OAuthAuthorization, OneOf.Types.None>.FromT3(new OneOf.Types.None()),
                new Dictionary<string, string>(w.Target.Headers ?? new Dictionary<string, string>())),
            new EventSequences.EventSequenceId(w.EventSequenceId),
            w.IsReplayable,
            w.IsActive));
    }
}
