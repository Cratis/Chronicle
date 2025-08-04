// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Represents a kernel reactor that will process events.
/// </summary>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reactors)]
public class Reactor : Grain<ReactorDefinition>, IReactor
{
    Dictionary<string, MethodInfo> _eventMethodsByEventType = new();

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var eventMethods = GetType().GetEventMethods();
        _eventMethodsByEventType = eventMethods.ToDictionary(
            method => method.GetEventType().Name,
            method => method);

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
                    var task = (method.Invoke(this, [@event, @event.Context]) as Task)!;
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
