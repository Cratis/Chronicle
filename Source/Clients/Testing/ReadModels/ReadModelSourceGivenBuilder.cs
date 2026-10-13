// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a builder for seeding events for a specific <see cref="EventSourceId"/> into a <see cref="ReadModelScenario{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">The type of read model under test.</typeparam>
/// <param name="scenario">The <see cref="ReadModelScenario{TReadModel}"/> to drive.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the seeded events.</param>
public class ReadModelSourceGivenBuilder<TReadModel>(ReadModelScenario<TReadModel> scenario, EventSourceId eventSourceId)
    where TReadModel : class
{
    readonly EventSourceType _eventSourceType = EventSourceType.Default;
    readonly EventStreamType _eventStreamType = EventStreamType.All;
    readonly EventStreamId _eventStreamId = EventStreamId.Default;
    readonly IReadOnlyList<string> _tags = [];

    ReadModelSourceGivenBuilder(
        ReadModelScenario<TReadModel> scenario,
        EventSourceId eventSourceId,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        IReadOnlyList<string> tags)
        : this(scenario, eventSourceId)
    {
        _eventSourceType = eventSourceType;
        _eventStreamType = eventStreamType;
        _eventStreamId = eventStreamId;
        _tags = tags;
    }

    /// <summary>
    /// Creates a builder that seeds events on the specified event source and stream route.
    /// </summary>
    /// <param name="eventSourceType">The event source type, or <see langword="null"/> for <see cref="EventSourceType.Default"/>.</param>
    /// <param name="eventStreamType">The event stream type, or <see langword="null"/> for <see cref="EventStreamType.All"/>.</param>
    /// <param name="eventStreamId">The event stream identifier, or <see langword="null"/> for <see cref="EventStreamId.Default"/>.</param>
    /// <returns>A new builder carrying the route, leaving this builder unchanged.</returns>
    /// <remarks>
    /// The route is included in each seeded event's context. Omitted values use the same defaults as
    /// appending to the event log. It does not affect pre-seeded read model instances.
    /// <code language="csharp">
    /// await scenario.Given.ForEventSource(orderId)
    ///     .OnStream(eventSourceType: "Order", eventStreamType: "Payments", eventStreamId: "monthly")
    ///     .Events(new PaymentReceived(100));
    /// </code>
    /// </remarks>
    public ReadModelSourceGivenBuilder<TReadModel> OnStream(
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default) =>
        new(scenario, eventSourceId, eventSourceType ?? EventSourceType.Default, eventStreamType ?? EventStreamType.All, eventStreamId ?? EventStreamId.Default, _tags);

    /// <summary>
    /// Creates a builder that associates append tags with its seeded events.
    /// </summary>
    /// <param name="tags">Tags to combine with existing builder tags and static event tags.</param>
    /// <returns>A new builder carrying a snapshot of the tags, leaving this builder unchanged.</returns>
    public ReadModelSourceGivenBuilder<TReadModel> WithTags(params string[] tags) =>
        new(scenario, eventSourceId, _eventSourceType, _eventStreamType, _eventStreamId, _tags.Concat(tags).Distinct(StringComparer.Ordinal).ToArray());

    /// <summary>
    /// Collects the provided event instances for this event source to be processed together with all other
    /// collected events when <see cref="ReadModelScenario{TReadModel}.Instance"/> is first accessed.
    /// </summary>
    /// <remarks>
    /// Events are not processed immediately. Deferred processing enables multi-stream scenarios where events
    /// across different event sources are required (for example, <c language="csharp">ChildrenFrom</c> projections that link
    /// child entities to parents via a parent key on a separate event source stream).
    /// </remarks>
    /// <param name="events">The event instances to collect in order.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task Events(params object[] events)
    {
        scenario.CollectEventsFor(eventSourceId, events, _eventSourceType, _eventStreamType, _eventStreamId, _tags);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers a pre-built read model instance for this event source, making it available through
    /// <see cref="ReadModelScenario{TReadModel}.ReadModels"/> for <c language="csharp">GetInstanceById</c> calls.
    /// </summary>
    /// <remarks>
    /// Use this when testing production code that calls <c language="csharp">IReadModels.GetInstanceById</c> — seed the
    /// expected read model state here rather than replaying events through a projection.
    /// </remarks>
    /// <param name="readModel">The read model instance to register for this event source.</param>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task ReadModel(TReadModel readModel)
    {
        scenario.CollectReadModelFor(eventSourceId, readModel);
        return Task.CompletedTask;
    }
}
