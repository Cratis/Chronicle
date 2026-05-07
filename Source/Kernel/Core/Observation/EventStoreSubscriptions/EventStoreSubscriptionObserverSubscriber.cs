// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreSubscriptionObserverSubscriber"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for accessing event sequence grains.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serializing event content.</param>
/// <param name="logger">The logger.</param>
public class EventStoreSubscriptionObserverSubscriber(
    IGrainFactory grainFactory,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<EventStoreSubscriptionObserverSubscriber> logger) : Grain, IEventStoreSubscriptionObserverSubscriber
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
        if (context.Metadata is not string targetEventStoreValue || string.IsNullOrEmpty(targetEventStoreValue))
        {
            logger.MissingTargetEventStore(_key);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                ["Missing target event store in subscriber context"],
                string.Empty);
        }

        var targetEventStore = new EventStoreName(targetEventStoreValue);
        var inboxSequenceId = new EventSequenceId($"{EventSequenceId.InboxPrefix}{_key.EventStore}");
        var inboxSequence = grainFactory.GetEventSequence(inboxSequenceId, targetEventStore, _key.Namespace);

        try
        {
            foreach (var @event in events)
            {
                var content = SerializeContent(@event.Content);
                await inboxSequence.Append(
                    @event.Context.EventSourceType,
                    @event.Context.EventSourceId,
                    @event.Context.EventStreamType,
                    @event.Context.EventStreamId,
                    @event.Context.EventType,
                    content,
                    @event.Context.CorrelationId,
                    @event.Context.Causation,
                    @event.Context.CausedBy,
                    [],
                    ConcurrencyScope.None,
                    subject: @event.Context.Subject);
            }

            logger.SuccessfullyForwardedEvents(_key, targetEventStore, inboxSequenceId);
            return ObserverSubscriberResult.Ok(events.LastOrDefault()?.Context.SequenceNumber ?? EventSequenceNumber.Unavailable);
        }
        catch (Exception ex)
        {
            logger.ErrorForwardingEvents(ex, _key, targetEventStore, inboxSequenceId);
            return new ObserverSubscriberResult(
                ObserverSubscriberState.Failed,
                EventSequenceNumber.Unavailable,
                [ex.Message],
                ex.StackTrace ?? string.Empty);
        }
    }

    JsonObject SerializeContent(System.Dynamic.ExpandoObject content)
    {
        var json = JsonSerializer.Serialize(content, jsonSerializerOptions);
        return JsonNode.Parse(json) as JsonObject ?? new JsonObject();
    }
}
