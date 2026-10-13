// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Execution;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Processes events through a reducer to produce a read model instance for testing.
/// </summary>
internal static class ReducerReadModelProcessor
{
    /// <summary>
    /// Gets a value indicating whether this processor runs Chronicle's compliance and security stack.
    /// </summary>
    /// <remarks>
    /// A deployed Chronicle encrypts a reduced read model's <c language="csharp">[PII]</c> and <c language="csharp">[Encrypted]</c> members
    /// on the way into the sink and releases them on the way out. Nothing below does either — the reducer's own
    /// result is what a spec reads. Wire compliance and security in and flip this, and <see cref="SubstitutedLayers"/>
    /// stops reporting <see cref="ReadModelSubstitutedLayer.Compliance"/>.
    /// </remarks>
    public static bool AppliesCompliance => false;

    /// <summary>
    /// Processes the given events through the reducer for <typeparamref name="TReadModel"/> and returns the resulting read model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model produced by the reducer.</typeparam>
    /// <param name="reducerType">The registered classic or self-reducer declaration.</param>
    /// <param name="events">The events to process.</param>
    /// <param name="eventStore">The scenario's shared test store.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for looking up event type metadata.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> used when invoking the reducer.</param>
    /// <param name="initialState">Optional initial read model state the reducer reduces on top of.</param>
    /// <param name="eventContexts">Optional original contexts when reading the store's event log.</param>
    /// <param name="readModelType">Optional runtime read model type for an untyped fold.</param>
    /// <returns>The reduced read model and the distinct event sources that passed the reducer's filters.</returns>
    /// <exception cref="ReducerFailed">Thrown when the reducer raises an error while processing the events.</exception>
    public static async Task<(TReadModel? State, IReadOnlyList<EventSourceId> EventSourceIds)> Process<TReadModel>(
        Type reducerType,
        IEnumerable<EventForEventSourceId> events,
        EventStoreForTesting eventStore,
        IEventTypes eventTypes,
        IServiceProvider serviceProvider,
        TReadModel? initialState = null,
        IReadOnlyList<EventContext>? eventContexts = null,
        Type? readModelType = null)
        where TReadModel : class
    {
        readModelType ??= typeof(TReadModel);
        var invoker = eventStore.Reducers.GetHandlerForReadModelType(readModelType).Invoker;

        var eventsList = events.ToList();
        var contexts = eventContexts ?? eventsList.Select((@event, index) =>
        {
            return EventContext.From(
                eventStore.Name,
                eventStore.Namespace,
                eventTypes.GetEventTypeFor(@event.Event.GetType()),
                @event.EventSourceType,
                @event.EventSourceId,
                @event.EventStreamType,
                @event.EventStreamId,
                (EventSequenceNumber)(ulong)index,
                CorrelationId.NotSet) with
            {
                Tags = @event.Event.GetType().GetTags().Concat(@event.Tags).Distinct(StringComparer.Ordinal).Select(tag => new Tag(tag)).ToArray()
            };
        }).ToArray();
        var (prepared, _) = await ReadModelEvents.Prepare(eventStore, eventsList.Select(input => (input.EventSourceId, input.Event)).ToArray(), contexts, persist: eventContexts is null);
        var filters = new ObserverRouteFilter(reducerType, eventStore.EventSources, includeTagFilters: true);
        var eventsAndContexts = new List<EventAndContext>();
        foreach (var index in Enumerable.Range(0, prepared.Length).Where(index => filters.MatchesClient(contexts[index])))
        {
            var delivered = prepared[index];
            var type = new EventType(new(delivered.Context.EventType.Id.Value), new(delivered.Context.EventType.Generation.Value));
            var json = JsonSerializer.SerializeToNode(delivered.Content)!.AsObject();
            var content = await eventStore.EventSerializer.Deserialize(eventTypes.GetClrTypeFor(type.Id, type.Generation), json);
            eventsAndContexts.Add(new(content, contexts[index] with { EventType = type }));
        }

        var result = await invoker.Invoke(serviceProvider, eventsAndContexts, initialState);

        // A reducer that raised an error must not surface its partial state as a successful projection —
        // the real reducer pipeline discards the result and pauses the partition. Fail loud instead so the
        // reducer bug is visible in the spec rather than masquerading as a (wrong) read model.
        if (!result.IsSuccess)
        {
            throw new ReducerFailed(readModelType, result.ErrorMessages, result.StackTrace);
        }

        return (result.ReadModelState as TReadModel, eventsAndContexts.Select(e => e.Context.EventSourceId).Distinct().ToArray());
    }
}
