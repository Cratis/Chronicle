// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Execution;
using Cratis.Serialization;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Processes events through a reducer to produce a read model instance for testing.
/// </summary>
internal static class ReducerReadModelProcessor
{
    /// <summary>
    /// Processes the given events through the reducer for <typeparamref name="TReadModel"/> and returns the resulting read model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model produced by the reducer.</typeparam>
    /// <param name="reducerType">The reducer type implementing <see cref="IReducerFor{TReadModel}"/>.</param>
    /// <param name="events">The events to process.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for looking up event type metadata.</param>
    /// <param name="artifactsActivator"><see cref="IClientArtifactsActivator"/> for instantiating the reducer.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> used when invoking the reducer.</param>
    /// <param name="namingPolicy">The <see cref="INamingPolicy"/> for deriving the read model container name.</param>
    /// <returns>The projected read model, or <see langword="null"/> if the reducer removed the instance.</returns>
    public static async Task<TReadModel?> Process<TReadModel>(
        Type reducerType,
        IEnumerable<EventForEventSourceId> events,
        IEventTypes eventTypes,
        IClientArtifactsActivator artifactsActivator,
        IServiceProvider serviceProvider,
        INamingPolicy namingPolicy)
        where TReadModel : class
    {
        var readModelType = typeof(TReadModel);
        var containerName = (ReadModelContainerName)namingPolicy.GetReadModelName(readModelType);
        var invoker = new ReducerInvoker(eventTypes, artifactsActivator, reducerType, readModelType, containerName);

        var eventsAndContexts = events.Select(@event =>
        {
            var context = EventContext.From(
                EventStoreName.NotSet,
                EventStoreNamespaceName.NotSet,
                eventTypes.GetEventTypeFor(@event.Event.GetType()),
                @event.EventSourceType,
                @event.EventSourceId,
                @event.EventStreamType,
                @event.EventStreamId,
                EventSequenceNumber.First,
                CorrelationId.NotSet);

            return new EventAndContext(@event.Event, context);
        });

        var result = await invoker.Invoke(serviceProvider, eventsAndContexts, null);
        return result.ReadModelState as TReadModel;
    }
}
