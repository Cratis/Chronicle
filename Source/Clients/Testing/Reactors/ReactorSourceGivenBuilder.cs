// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents a builder for providing events for a specific <see cref="EventSourceId"/> to a <see cref="ReactorScenario{TReactor}"/>.
/// </summary>
/// <typeparam name="TReactor">The type of reactor under test.</typeparam>
/// <param name="scenario">The <see cref="ReactorScenario{TReactor}"/> to drive.</param>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> to associate with the provided events.</param>
public class ReactorSourceGivenBuilder<TReactor>(ReactorScenario<TReactor> scenario, EventSourceId eventSourceId)
    where TReactor : class
{
    readonly EventSourceType _eventSourceType = EventSourceType.Default;
    readonly EventStreamType _eventStreamType = EventStreamType.All;
    readonly EventStreamId _eventStreamId = EventStreamId.Default;

    ReactorSourceGivenBuilder(
        ReactorScenario<TReactor> scenario,
        EventSourceId eventSourceId,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId)
        : this(scenario, eventSourceId)
    {
        _eventSourceType = eventSourceType;
        _eventStreamType = eventStreamType;
        _eventStreamId = eventStreamId;
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
    /// </remarks>
    public ReactorSourceGivenBuilder<TReactor> OnStream(
        EventSourceType? eventSourceType = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default) =>
        new(scenario, eventSourceId, eventSourceType ?? EventSourceType.Default, eventStreamType ?? EventStreamType.All, eventStreamId ?? EventStreamId.Default);

    /// <summary>
    /// Provides the given event instances to the reactor and awaits their handling.
    /// </summary>
    /// <remarks>
    /// A fresh instance of <typeparamref name="TReactor"/> is activated from the service provider and
    /// each event is passed to the reactor in order. Side-effects are observable through mocks or
    /// other test doubles registered in the service provider.
    /// </remarks>
    /// <param name="events">The event instances to provide to the reactor, in order.</param>
    /// <returns>A <see cref="Task"/> that completes when the reactor has handled all events.</returns>
    /// <exception cref="ReadModelNotSeededForReactorScenario">Thrown when a read-model parameter was neither seeded nor declared absent.</exception>
    public Task Events(params object[] events) =>
        scenario.InvokeWith(eventSourceId, events, _eventSourceType, _eventStreamType, _eventStreamId);

    /// <summary>
    /// Records one event with its scenario-local context without delivering it or appending it to an event store.
    /// </summary>
    /// <param name="event">The event to record.</param>
    /// <returns>The recorded occurrence for later redelivery.</returns>
    public async Task<RecordedOccurrence> History(object @event) =>
        (await History([@event]))[0];

    /// <summary>
    /// Records events in order without delivering them or appending them to an event store.
    /// </summary>
    /// <param name="events">The events to record.</param>
    /// <returns>The occurrences with their original contexts for later redelivery.</returns>
    public Task<IReadOnlyList<RecordedOccurrence>> History(params object[] events) =>
        scenario.RecordHistory(eventSourceId, events, _eventSourceType, _eventStreamType, _eventStreamId);

    /// <summary>
    /// Declares that a read model is deliberately absent for this key, allowing the handler to receive null.
    /// </summary>
    /// <remarks>
    /// Use the key returned by <see cref="ICanResolveReadModelKey"/> when the reactor
    /// implements it; otherwise use the triggering event source identifier. This declaration is shared across
    /// the scenario's builders and is unaffected by <see cref="OnStream"/>. It does not remove a seeded instance.
    /// </remarks>
    /// <typeparam name="TReadModel">The type of read model expected to be absent.</typeparam>
    /// <returns>This <see cref="ReactorSourceGivenBuilder{TReactor}"/> for further chaining.</returns>
    public ReactorSourceGivenBuilder<TReactor> NoReadModel<TReadModel>()
        where TReadModel : class
    {
        scenario.DeclareNoReadModel<TReadModel>(eventSourceId);
        return this;
    }

    /// <summary>
    /// Seeds a pre-built read model instance for this event source, materializing the reactor's read-model
    /// handler-method parameter of the same type with it.
    /// </summary>
    /// <remarks>
    /// Use this when the reactor takes a read model as a handler-method parameter — seed the expected state here
    /// rather than replaying events through a projection. Chain <c language="csharp">.Events(...)</c> afterwards to drive the reactor.
    /// </remarks>
    /// <typeparam name="TReadModel">The type of read model to seed.</typeparam>
    /// <param name="readModel">The read model instance to seed for this event source.</param>
    /// <returns>This <see cref="ReactorSourceGivenBuilder{TReactor}"/> for further chaining.</returns>
    public ReactorSourceGivenBuilder<TReactor> ReadModel<TReadModel>(TReadModel readModel)
        where TReadModel : class
    {
        scenario.SeedReadModel(eventSourceId, readModel);
        return this;
    }
}
