// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Grains.EventTypes.Kernel;
using Cratis.Chronicle.Json;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Represents a kernel reactor that will process events.
/// </summary>
/// <param name="eventTypes"><see cref="IEventTypes"/> for working with event types.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between expando objects to and from json.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reactors)]
public class Reactor(IEventTypes eventTypes, IExpandoObjectConverter expandoObjectConverter) : Grain<ReactorDefinition>, IReactor
{
    Dictionary<string, MethodInfo> _eventMethodsByEventType = new();
    Dictionary<string, Type> _eventTypeByEventType = new();

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var eventMethods = GetType().GetEventMethods();
        _eventMethodsByEventType = eventMethods.ToDictionary(
            method => method.GetEventType().Name,
            method => method);

        _eventTypeByEventType = eventMethods.ToDictionary(
            method => method.GetEventType().Name,
            method => method.GetParameters()[0].ParameterType);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events)
    {
        foreach (var @event in events)
        {
            if (_eventMethodsByEventType.TryGetValue(@event.Metadata.Type.Id, out var method))
            {
                try
                {
                    var eventType = _eventTypeByEventType[@event.Metadata.Type.Id];
                    var content = expandoObjectConverter.ToJsonObject(@event.Content, eventTypes.GetJsonSchema(eventType));
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
