// Copyright (c) Cratis. All rights reserved.
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ReducerHandler"/> class.
    /// </summary>
    /// <param name="reducerId">The identifier of the reducer.</param>
    /// <param name="name">The name of the reducer.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the reducer is for.</param>
    /// <param name="invoker">The actual invoker.</param>
    /// <param name="eventSerializer">The event serializer to use.</param>
    /// <param name="isActive">Whether or not reducer should be actively running on the Kernel.</param>
    public ReducerHandler(
        ReducerId reducerId,
        ObserverName name,
        EventSequenceId eventSequenceId,
        IReducerInvoker invoker,
        IEventSerializer eventSerializer,
        bool isActive)
    {
        ReducerId = reducerId;
        Name = name;
        EventSequenceId = eventSequenceId;
        _reducerInvoker = invoker;
        _eventSerializer = eventSerializer;
        IsActive = isActive;
        Invoker = invoker;
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
    public bool IsActive { get; }

    /// <inheritdoc/>
    public IReducerInvoker Invoker { get; }

    /// <inheritdoc/>
    public async Task<InternalReduceResult> OnNext(IEnumerable<AppendedEvent> events, object? initial)
    {
        var tasks = events.Select(async @event =>
        {
            var content = await _eventSerializer.Deserialize(@event);
            return new EventAndContext(content, @event.Context);
        });
        var eventAndContexts = await Task.WhenAll(tasks.ToArray()!);
        return await _reducerInvoker.Invoke(eventAndContexts, initial);
    }
}
