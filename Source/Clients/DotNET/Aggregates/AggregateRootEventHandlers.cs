// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Aksio.Cratis.Conventions;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents the event handlers for an <see cref="AggregateRoot"/>.
/// </summary>
public class AggregateRootEventHandlers
{
    readonly Dictionary<Type, MethodInfo> _handleMethodsByEventType;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootEventHandlers"/> class.
    /// </summary>
    /// <param name="aggregateRootType">Type of <see cref="IAggregateRoot"/>.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    public AggregateRootEventHandlers(Type aggregateRootType, IEventTypes eventTypes)
    {
        _handleMethodsByEventType = aggregateRootType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                        .Where(_ => _.IsEventHandlerMethod())
                                        .ToDictionary(_ => _.GetParameters()[0].ParameterType, _ => _);

        EventTypes = _handleMethodsByEventType.Keys.Select(_ => eventTypes.GetEventTypeFor(_)!).ToImmutableList();
    }

    /// <summary>
    /// Gets whether or not it has any handle methods.
    /// </summary>
    public bool HasHandleMethods => _handleMethodsByEventType.Count > 0;

    /// <summary>
    /// Gets a collection of <see cref="EventType">event types</see> that can be handled.
    /// </summary>
    public IImmutableList<EventType> EventTypes { get; }

    /// <summary>
    /// Handle a collection of events.
    /// </summary>
    /// <param name="target">The target <see cref="IAggregateRoot"/> to handle for.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to handle.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handle(IAggregateRoot target, IEnumerable<EventAndContext> events)
    {
        foreach (var eventAndContext in events)
        {
            var eventType = eventAndContext.Event.GetType();
            if (_handleMethodsByEventType.TryGetValue(eventType, out var method))
            {
                Task returnValue;
                var parameters = method.GetParameters();

                if (parameters.Length == 2)
                {
                    returnValue = (Task)method.Invoke(target, new object[] { eventAndContext.Event, eventAndContext.Context })!;
                }
                else
                {
                    returnValue = (Task)method.Invoke(target, new object[] { eventAndContext.Event })!;
                }

                if (returnValue is not null) await returnValue;
            }
        }
    }
}
