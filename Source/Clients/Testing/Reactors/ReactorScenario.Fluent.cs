// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Execution;

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
    EventSequenceNumber _nextSequenceNumber = EventSequenceNumber.First;

    /// <summary>
    /// Gets the builder for providing event history to this reactor.
    /// </summary>
    public ReactorScenarioGivenBuilder Given => new(this);

    /// <summary>
    /// Invokes applicable callbacks for the supplied events.
    /// </summary>
    /// <param name="eventSourceId">The event source identifier.</param>
    /// <param name="events">The events to provide in order.</param>
    /// <returns>A task completing after all callbacks complete.</returns>
    internal async Task InvokeWith(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        var definition = _definition.Value;
        foreach (var @event in events)
        {
            var eventType = defaults.EventTypes.GetEventTypeFor(@event.GetType());
            var context = EventContext.From(
                defaults.EventStore.Name,
                defaults.EventStore.Namespace,
                eventType,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                _nextSequenceNumber,
                CorrelationId.New());
            _nextSequenceNumber += 1;
            await definition.Handle(@event, context);
        }
    }

    static IReactorDefinition CreateDefinition(ReactorId id, Defaults defaults)
    {
        var registration = defaults.Options.ExplicitArtifacts.Reactors.FirstOrDefault(_ => _.Id == id);
        if (registration.Define is null)
        {
            throw new UnknownReactorId(id);
        }

        var builder = new ReactorBuilder(defaults.EventTypes);
        registration.Define(builder);
        return builder.Build(id);
    }
}
