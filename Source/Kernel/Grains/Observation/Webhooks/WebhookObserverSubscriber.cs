// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Microsoft.Extensions.Logging;
using OneOf.Types;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="IWebhookObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WebhookObserverSubscriber"/> class.
/// </remarks>
/// <param name="webhookMediator">The <see cref="IWebhookMediator"/>.</param>
/// <param name="logger">The logger.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Webhooks)]
public class WebhookObserverSubscriber(
    IWebhookMediator webhookMediator,
    ILogger<WebhookObserverSubscriber> logger) : Grain<WebhookDefinition>, IWebhookObserverSubscriber, INotifyWebhookDefinitionsChanged
{
    ObserverKey _key = ObserverKey.NotSet;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        (_key, _) = this.GetKeys();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var onNextResult = await webhookMediator.OnNext(State.Target, partition, events);
        return onNextResult.Match(HandleSuccess, HandleError);

        ObserverSubscriberResult HandleSuccess(None none)
        {
            logger.SuccessfullyHandledAllEvents(_key);
            return ObserverSubscriberResult.Ok(events.LastOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable);
        }
        ObserverSubscriberResult HandleError(Exception error)
        {
            logger.ErrorHandling(error, _key);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                error.GetAllMessages(),
                error.StackTrace ?? string.Empty);
        }
    }

    /// <inheritdoc/>
    public Task OnWebhookDefinitionsChanged() => ReadStateAsync();
}
