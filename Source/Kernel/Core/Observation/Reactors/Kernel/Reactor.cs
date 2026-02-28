// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Reactors.Kernel;

/// <summary>
/// Represents a kernel reactor that will process events.
/// </summary>
public class Reactor : IReactor
{
    Dictionary<string, MethodInfo> _eventMethodsByEventType = new();
    IEventSerializer _eventSerializer = default!;

    /// <inheritdoc/>
    public void Initialize(IEventSerializer eventSerializer)
    {
        _eventSerializer = eventSerializer;

        var eventMethods = GetType().GetEventMethods();
        _eventMethodsByEventType = eventMethods.ToDictionary(
            method => method.GetEventType().Name,
            method => method);
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events)
    {
        var lastSuccessfullyHandledSequenceNumber = EventSequenceNumber.Unavailable;

        foreach (var @event in events)
        {
            if (_eventMethodsByEventType.TryGetValue(@event.Context.EventType.Id, out var method))
            {
                try
                {
                    var content = _eventSerializer.Deserialize(@event);
                    var task = (method.Invoke(this, [content, @event.Context]) as Task)!;
                    await task;
                    lastSuccessfullyHandledSequenceNumber = @event.Context.SequenceNumber;
                }
                catch (Exception exception)
                {
                    return new(
                        ObserverSubscriberState.Failed,
                        lastSuccessfullyHandledSequenceNumber,
                        exception.GetAllMessages(),
                        exception.StackTrace ?? string.Empty);
                }
            }
        }

        return ObserverSubscriberResult.Ok(events.Last().Context.SequenceNumber);
    }
}
