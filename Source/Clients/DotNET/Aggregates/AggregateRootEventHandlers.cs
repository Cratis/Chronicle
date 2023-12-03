// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Aksio.Cratis.Conventions;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootEventHandlers"/>.
/// </summary>
public class AggregateRootEventHandlers : IAggregateRootEventHandlers
{
    readonly Dictionary<Type, MethodInfo> _handleMethodsByEventType;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootEventHandlers"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>
    /// <param name="aggregateRootType">Type of <see cref="IAggregateRoot"/>.</param>
    public AggregateRootEventHandlers(IEventTypes eventTypes, Type aggregateRootType)
    {
        _handleMethodsByEventType = aggregateRootType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                        .Where(_ => _.IsEventHandlerMethod(eventTypes.AllClrTypes))
                                        .ToDictionary(_ => _.GetParameters()[0].ParameterType, _ => _);

        EventTypes = _handleMethodsByEventType.Keys.Select(_ => _.GetEventType()).ToImmutableList();
    }

    /// <inheritdoc/>
    public bool HasHandleMethods => _handleMethodsByEventType.Count > 0;

    /// <inheritdoc/>
    public IImmutableList<EventType> EventTypes { get; }

    /// <inheritdoc/>
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
