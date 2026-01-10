// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represent an implementation of <see cref="IWebhookDefinitionComparer"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="objectComparer">The <see cref="IObjectComparer"/>.</param>
/// <param name="logger">The logger.</param>
[Singleton]
public class WebhookDefinitionComparer(
    IStorage storage,
    IObjectComparer objectComparer,
    ILogger<WebhookDefinitionComparer> logger) : IWebhookDefinitionComparer
{
    /// <inheritdoc/>
    public async Task<WebhookDefinitionCompareResult> Compare(
        WebhookKey webhookKey,
        WebhookDefinition first,
        WebhookDefinition second)
    {
        if (!await storage.GetEventStore(webhookKey.EventStore).Webhooks.Has(webhookKey.WebhookId))
        {
            logger.WebhookIsNew(webhookKey.WebhookId);
            return WebhookDefinitionCompareResult.New;
        }

        logger.ComparingDefinitions(webhookKey.WebhookId);

        return objectComparer.Compare(first, second, out _)
            ? WebhookDefinitionCompareResult.Same
            : WebhookDefinitionCompareResult.Different;
    }
}
