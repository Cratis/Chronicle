// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.EventTypes.Kernel;
using Cratis.Chronicle.Json;
using Cratis.Json;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Represents a kernel reactor that will process events.
/// </summary>
public class Reactor : IReactor
{
    Dictionary<string, MethodInfo> _eventMethodsByEventType = new();
    Dictionary<string, Type> _eventTypeByEventType = new();
    IEventTypes _eventTypes = default!;
    IExpandoObjectConverter _expandoObjectConverter = default!;

    /// <inheritdoc/>
    public void Initialize(IEventTypes eventTypes, IExpandoObjectConverter expandoObjectConverter)
    {
        _eventTypes = eventTypes;
        _expandoObjectConverter = expandoObjectConverter;

        var eventMethods = GetType().GetEventMethods();
        _eventMethodsByEventType = eventMethods.ToDictionary(
            method => method.GetEventType().Name,
            method => method);

        _eventTypeByEventType = eventMethods.ToDictionary(
            method => method.GetEventType().Name,
            method => method.GetParameters()[0].ParameterType);
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events)
    {
        foreach (var @event in events)
        {
            if (_eventMethodsByEventType.TryGetValue(@event.Context.EventType.Id, out var method))
            {
                try
                {
                    var eventType = _eventTypeByEventType[@event.Context.EventType.Id];
                    var contentAsJson = _expandoObjectConverter.ToJsonObject(@event.Content, _eventTypes.GetJsonSchema(eventType));
                    var content = contentAsJson.Deserialize(eventType, Globals.JsonSerializerOptions);
                    var task = (method.Invoke(this, [content, @event.Context]) as Task)!;
                    await task;
                }
                catch (Exception exception)
                {
                    return new(
                        ObserverSubscriberState.Failed,
                        @event.Context.SequenceNumber,
                        exception.GetAllMessages(),
                        exception.StackTrace ?? string.Empty);
                }
            }
        }

        return ObserverSubscriberResult.Ok(events.Last().Context.SequenceNumber);
    }
}
