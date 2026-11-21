// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the context for an observer subscriber.
/// </summary>
/// <param name="Metadata">Optional metadata associated.</param>
public record ObserverSubscriberContext(object? Metadata)
{
    /// <summary>
    /// Gets the deadline after which the caller no longer accepts a result.
    /// </summary>
    public DateTimeOffset? DeliverBy { get; init; }

    /// <summary>
    /// Gets the deadline after which the subscriber stops holding the abandoned call open.
    /// </summary>
    public DateTimeOffset? AbandonAt { get; init; }

    /// <summary>
    /// Gets the path the events being delivered came from.
    /// </summary>
    public ObservationOrigin Origin { get; init; }

    /// <summary>
    /// Gets the replay operation that scheduled the delivery, or null for live and catch-up deliveries.
    /// It is an input of the deterministic webhook message id (see <c language="csharp">WebhookMessageIds</c>).
    /// </summary>
    public ReplayEpoch? ReplayEpoch { get; init; }

    /// <summary>
    /// Gets the stored events before compliance release, for a subscriber resolving a different generation.
    /// </summary>
    public IReadOnlyList<AppendedEvent> UnreleasedEvents { get; init; } = [];

    /// <summary>
    /// Retains stored content only for subscribers that can resolve a new generation at delivery time.
    /// </summary>
    /// <param name="events">The events before compliance release.</param>
    /// <param name="subscriberType">The subscriber type.</param>
    /// <returns>The delivery context with the stored events when needed.</returns>
    public ObserverSubscriberContext WithUnreleasedEvents(IEnumerable<AppendedEvent> events, Type subscriberType) =>
        subscriberType.IsAssignableTo(typeof(IWebhookObserverSubscriber)) || subscriberType.IsAssignableTo(typeof(IEventStoreSubscriptionObserverSubscriber))
            ? this with { UnreleasedEvents = events.ToArray() }
            : this;

    /// <summary>
    /// Gets stored content while retaining the incoming delivery context, including replay state.
    /// </summary>
    /// <param name="incoming">The event being delivered.</param>
    /// <returns>The unreleased event with the incoming context.</returns>
    public AppendedEvent? GetUnreleasedEvent(AppendedEvent incoming)
    {
        var stored = UnreleasedEvents.SingleOrDefault(@event => @event.Context.SequenceNumber == incoming.Context.SequenceNumber);
        return stored is null ? null : stored with { Context = incoming.Context with { EventType = stored.Context.EventType, Hash = stored.Context.Hash } };
    }

    /// <summary>
    /// Gets how long is left before the caller stops accepting a result.
    /// </summary>
    /// <param name="clock">The clock.</param>
    /// <returns>The time left, zero when the deadline has passed, or null when the delivery has no deadline.</returns>
    public TimeSpan? RemainingDeliveryTime(TimeProvider clock)
    {
        if (DeliverBy is not { } deliverBy) return null;
        var remaining = deliverBy - clock.GetUtcNow();
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Adds fresh deadlines for this dispatch, leaving disabled deliveries unbounded.
    /// </summary>
    /// <param name="timeout">The caller's timeout.</param>
    /// <param name="hold">The abandoned delivery hold.</param>
    /// <param name="clock">The clock.</param>
    /// <returns>The context for this dispatch.</returns>
    public ObserverSubscriberContext WithDeadlines(TimeSpan timeout, TimeSpan hold, TimeProvider clock) =>
        timeout > TimeSpan.Zero && hold > TimeSpan.Zero
            ? this with { DeliverBy = clock.GetUtcNow() + timeout, AbandonAt = clock.GetUtcNow() + timeout + hold }
            : this with { DeliverBy = null, AbandonAt = null };
}
