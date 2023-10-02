// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerHandler"/>.
/// </summary>
public class ReducerHandler : IReducerHandler
{
    readonly IReducerInvoker _reducerInvoker;
    readonly IEventSerializer _eventSerializer;
    readonly IEventTypes _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerHandler"/> class.
    /// </summary>
    /// <param name="reducerId">The identifier of the reducer.</param>
    /// <param name="name">The name of the reducer.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the reducer is for.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/>.</param>
    /// <param name="reducerInvoker">The actual invoker.</param>
    /// <param name="eventSerializer">The event serializer to use.</param>
    public ReducerHandler(
        ReducerId reducerId,
        ObserverName name,
        EventSequenceId eventSequenceId,
        IEventTypes eventTypes,
        IReducerInvoker reducerInvoker,
        IEventSerializer eventSerializer)
    {
        ReducerId = reducerId;
        Name = name;
        EventSequenceId = eventSequenceId;
        _eventTypes = eventTypes;
        _reducerInvoker = reducerInvoker;
        _eventSerializer = eventSerializer;
    }

    /// <inheritdoc/>
    public ReducerId ReducerId { get; }

    /// <inheritdoc/>
    public ObserverName Name { get; }

    /// <inheritdoc/>
    public EventSequenceId EventSequenceId { get; }

    /// <inheritdoc/>
    public IEnumerable<EventType> EventTypes => _reducerInvoker.EventTypes;

    /// <inheritdoc/>
    public Type ReadModelType => _reducerInvoker.ReadModelType;

    /// <inheritdoc/>
    public async Task<InternalReduceResult> OnNext(IEnumerable<AppendedEvent> events, object? initial)
    {
        var tasks = events.Select(async @event =>
        {
            var eventType = _eventTypes.GetClrTypeFor(@event.Metadata.Type.Id);
            var content = await _eventSerializer.Deserialize(eventType, @event.Content);
            return new EventAndContext(content, @event.Context);
        });
        var eventAndContexts = await Task.WhenAll(tasks.ToArray()!);
        return await _reducerInvoker.Invoke(eventAndContexts, initial);
    }
}
