// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Represents an in-process scenario for a reactor registered through explicit artifacts.
/// </summary>
/// <param name="id">The identifier supplied to ExplicitArtifacts.RegisterReactor.</param>
/// <param name="defaults">The per-run settings and artifacts containing the reactor registration.</param>
/// <remarks>
/// Builds the fluent definition against this run's event types and awaits its callbacks directly.
/// This tests handler behavior, not observer delivery, replay, or transport registration.
/// </remarks>
public class ReactorScenario(ReactorId id, Defaults defaults)
{
    readonly Lazy<IReactorDefinition> _definition = new(() => CreateDefinition(id, defaults));
    readonly ScenarioOccurrenceRecorder _occurrences = new(defaults.EventTypes, defaults.EventStore.Name, defaults.EventStore.Namespace);

    /// <summary>
    /// Gets the builder for providing event history to this reactor.
    /// </summary>
    public ReactorScenarioGivenBuilder Given => new(this);

    /// <summary>
    /// Delivers a recorded occurrence to the applicable fluent callbacks using its original context.
    /// </summary>
    /// <param name="occurrence">The recorded event and context.</param>
    /// <returns>A task completing after all applicable callbacks complete.</returns>
    public Task Redeliver(RecordedOccurrence occurrence) => Deliver([occurrence]);

    /// <summary>
    /// Invokes applicable callbacks for the supplied events.
    /// </summary>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="events">The events to provide in order.</param>
    /// <param name="eventSourceType">The event source type for the input events.</param>
    /// <param name="eventStreamType">The event stream type for the input events.</param>
    /// <param name="eventStreamId">The event stream identifier for the input events.</param>
    /// <returns>A task completing after all callbacks complete.</returns>
    internal Task InvokeWith(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId) =>
        Deliver(_occurrences.Record(eventSourceId, events, eventSourceType, eventStreamType, eventStreamId));

    /// <summary>
    /// Allocates original contexts for history without invoking callbacks.
    /// </summary>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="events">The events to record in order.</param>
    /// <param name="eventSourceType">The event source type.</param>
    /// <param name="eventStreamType">The event stream type.</param>
    /// <param name="eventStreamId">The event stream identifier.</param>
    /// <returns>The recorded occurrences.</returns>
    internal Task<IReadOnlyList<RecordedOccurrence>> RecordHistory(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventSourceType eventSourceType,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId) =>
        Task.FromResult<IReadOnlyList<RecordedOccurrence>>(_occurrences.Record(eventSourceId, events, eventSourceType, eventStreamType, eventStreamId).ToArray());

    static IReactorDefinition CreateDefinition(ReactorId id, Defaults defaults)
    {
        var registration = ((Events.EventStoreForTesting)defaults.EventStore).ExplicitSnapshot.Reactors.FirstOrDefault(_ => _.Id == id);
        if (registration.Define is null)
        {
            throw new UnknownReactorId(id);
        }

        var builder = new ReactorBuilder(defaults.EventTypes);
        registration.Define(builder);
        return builder.Build(id);
    }

    async Task Deliver(IEnumerable<RecordedOccurrence> occurrences)
    {
        var definition = _definition.Value;
        foreach (var occurrence in occurrences)
        {
            await definition.Handle(occurrence.Event, occurrence.Context);
        }
    }
}
