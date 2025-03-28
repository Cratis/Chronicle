// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Chronicle.Conventions;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootEventHandlers"/>.
/// </summary>
public class AggregateRootEventHandlers : IAggregateRootEventHandlers
{
    readonly Dictionary<Type, MethodInfo> _methodsByEventType;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootEventHandlers"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    /// <param name="aggregateRootType">Type of <see cref="IAggregateRoot"/>.</param>
    public AggregateRootEventHandlers(IEventTypes eventTypes, Type aggregateRootType)
    {
        _methodsByEventType = aggregateRootType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                        .Where(_ => _.IsEventHandlerMethod(eventTypes.AllClrTypes))
                                        .SelectMany(_ => _.GetParameters()[0].ParameterType.GetEventTypes(eventTypes.AllClrTypes).Select(eventType => (eventType, method: _)))
                                        .ToDictionary(_ => _.eventType, _ => _.method);

        EventTypes = _methodsByEventType.Keys.Select(_ => _.GetEventType()).ToImmutableList();
    }

    /// <inheritdoc/>
    public bool HasHandleMethods => _methodsByEventType.Count > 0;

    /// <inheritdoc/>
    public IImmutableList<EventType> EventTypes { get; }

    /// <inheritdoc/>
    public async Task Handle(IAggregateRoot target, IEnumerable<EventAndContext> events, Action<EventAndContext>? onHandledEvent = default)
    {
        if (_methodsByEventType.Count == 0) return;

        foreach (var eventAndContext in events)
        {
            var eventType = eventAndContext.Event.GetType();
            if (_methodsByEventType.TryGetValue(eventType, out var method))
            {
                Task returnValue;
                var parameters = method.GetParameters();

                if (parameters.Length == 2)
                {
                    returnValue = (Task)method.Invoke(target, [eventAndContext.Event, eventAndContext.Context])!;
                }
                else
                {
                    returnValue = (Task)method.Invoke(target, [eventAndContext.Event])!;
                }

                if (returnValue is not null) await returnValue;
                onHandledEvent?.Invoke(eventAndContext);
            }
        }
    }
}
