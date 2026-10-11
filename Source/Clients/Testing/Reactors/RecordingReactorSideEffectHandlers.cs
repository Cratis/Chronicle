// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// An <see cref="IReactorSideEffectHandlers"/> that records the side effects a reactor returns — events and commands
/// alike — for assertion, instead of appending or executing them. Used by <see cref="ReactorScenario{TReactor}"/> when
/// no explicit handlers are supplied, so a return-style reactor can be asserted directly.
/// </summary>
/// <param name="isClaimedReturnType">Checks whether a registered host handler claims a synchronous return type.</param>
sealed class RecordingReactorSideEffectHandlers(Func<Type, bool> isClaimedReturnType) : IReactorSideEffectHandlers
{
    readonly List<object> _produced = [];
    readonly List<ProducedEvent> _producedEvents = [];

    /// <summary>
    /// Gets the side effects the reactor produced, flattened — collections are expanded and
    /// <see cref="EventForEventSourceId"/> wrappers are unwrapped to the underlying event.
    /// </summary>
    public IReadOnlyList<object> Produced => _produced;

    /// <summary>
    /// Gets the produced events and their resolved destinations, excluding commands and other non-event side effects.
    /// </summary>
    public IReadOnlyList<ProducedEvent> ProducedEvents => _producedEvents;

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, object value)
    {
        if (reactorContext.EventStore is not null)
        {
            return CanHandle(reactorContext, reactorContext.EventStore, value);
        }

        return true;
    }

    /// <inheritdoc/>
    public bool CanHandle(ReactorContext reactorContext, IEventStore eventStore, object value) => true;

    /// <inheritdoc/>
    public bool CanHandleReturnType(Type type) => isClaimedReturnType(type);

    /// <inheritdoc/>
    Task<Result<ReactorSideEffectFailure>> IReactorSideEffectHandlers.Handle(ReactorContext reactorContext, IEventStore eventStore, object value)
    {
        foreach (var item in Flatten(value))
        {
            Record(item, reactorContext, eventStore);
        }

        return Task.FromResult(Result.Success<ReactorSideEffectFailure>());
    }

    static IEnumerable<object> Flatten(object value)
    {
        switch (value)
        {
            case EventsWithConcurrencyScopes eventsWithConcurrencyScopes:
                foreach (var @event in eventsWithConcurrencyScopes.Events)
                {
                    yield return @event;
                }

                break;

            case IEnumerable<object> collection:
                foreach (var item in collection)
                {
                    foreach (var inner in Flatten(item))
                    {
                        yield return inner;
                    }
                }

                break;

            default:
                yield return value;
                break;
        }
    }

    void Record(object item, ReactorContext reactorContext, IEventStore eventStore)
    {
        var destination = item as EventForEventSourceId;
        var @event = destination?.Event ?? item;
        _produced.Add(@event);
        if (destination is null && !eventStore.EventTypes.HasFor(@event.GetType()))
        {
            return;
        }

        destination ??= new(reactorContext.GetEventSourceId(), @event)
        {
            EventSourceType = reactorContext.GetEventSourceType() ?? EventSourceType.Default,
            EventStreamType = reactorContext.GetEventStreamType() ?? EventStreamType.All,
            EventStreamId = reactorContext.GetEventStreamId() ?? EventStreamId.Default
        };
        var routing = destination.EventSource is null
            ? null
            : ResolvedEventRouting.Resolve(eventStore.EventSources, destination.EventSource, destination.EventStream, destination.EventSourceType, destination.EventStreamType);
        _producedEvents.Add(new(
            @event,
            destination.EventSourceId,
            routing?.SourceType ?? (string.IsNullOrEmpty(destination.EventSourceType.Value) ? EventSourceType.Default : destination.EventSourceType),
            routing?.StreamType ?? (string.IsNullOrEmpty(destination.EventStreamType.Value) ? EventStreamType.All : destination.EventStreamType),
            string.IsNullOrEmpty(destination.EventStreamId.Value) ? (EventStreamId)EventStreamId.Default : destination.EventStreamId));
    }
}
