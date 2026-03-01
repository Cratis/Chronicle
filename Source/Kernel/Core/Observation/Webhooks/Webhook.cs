// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.States;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Utilities;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhook"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Webhook"/> class.
/// </remarks>
/// <param name="webhookDefinitionComparer"><see cref="IWebhookDefinitionComparer"/> for comparing projection definitions.</param>
/// <param name="logger">Logger for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Webhooks)]
public class Webhook(
    IWebhookDefinitionComparer webhookDefinitionComparer,
    ILogger<Webhook> logger) : Grain<WebhookDefinition>, IWebhook
{
    readonly ObserverManager<INotifyWebhookDefinitionsChanged> _definitionObservers = new(TimeSpan.FromDays(365 * 4), logger);

    /// <inheritdoc/>
    public async Task SetDefinition(WebhookDefinition definition)
    {
        var key = WebhookKey.Parse(this.GetPrimaryKeyString());
        logger.SettingDefinition(key.WebhookId);
        var compareResult = await webhookDefinitionComparer.Compare(key, State, definition);

        State = definition;
        await WriteStateAsync();

        if (compareResult.Result == WebhookDefinitionCompareResult.Different)
        {
            logger.WebhookHasChanged(key.WebhookId);
            await _definitionObservers.Notify(notifier => notifier.OnWebhookDefinitionsChanged());
            var namespaceNames = await GrainFactory.GetGrain<INamespaces>(key.EventStore).GetAll();
            await AddReplayRecommendationForAllNamespaces(key, namespaceNames);
        }
    }

    /// <inheritdoc/>
    public Task<WebhookDefinition> GetDefinition() => Task.FromResult(State);

    /// <inheritdoc/>
    public Task SubscribeDefinitionsChanged(INotifyWebhookDefinitionsChanged subscriber)
    {
        _definitionObservers.Subscribe(subscriber, subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnsubscribeDefinitionsChanged(INotifyWebhookDefinitionsChanged subscriber)
    {
        _definitionObservers.Unsubscribe(subscriber);
        return Task.CompletedTask;
    }

    async Task AddReplayRecommendationForAllNamespaces(WebhookKey key, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            var recommendations =
                GrainFactory.GetRecommendationsManager(new EventStoreAndNamespace(key.EventStore, @namespace));

            await recommendations.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                "Webhook definition has changed.",
                new()
                {
                    ObserverId = key.WebhookId,
                    ObserverKey = new(key.WebhookId, key.EventStore, @namespace, State.EventSequenceId),
                    Reasons = [new WebhookDefinitionChangedReplayCandidateReason()]
                });
        }
    }
}
