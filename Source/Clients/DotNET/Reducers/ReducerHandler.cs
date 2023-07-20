// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerHandler"/>.
/// </summary>
public class ReducerHandler : IReducerHandler
{
    readonly IReducerInvoker _reducerInvoker;
    readonly IEventSerializer _eventSerializer;
    readonly IEventTypes _eventTypes;

    /// <inheritdoc/>
    public ReducerId ReducerId { get; }

    /// <inheritdoc/>
    public ReducerName Name { get; }

    /// <inheritdoc/>
    public EventSequenceId EventSequenceId { get; }

    /// <inheritdoc/>
    public IEnumerable<EventType> EventTypes => _reducerInvoker.EventTypes;

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
        ReducerName name,
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
    public async Task<object> OnNext(AppendedEvent @event, object? initial) =>
        await OnNextBulk(new[] { @event }, initial);

    /// <inheritdoc/>
    public async Task<object> OnNextBulk(IEnumerable<AppendedEvent> events, object? initial)
    {
        foreach (var @event in events)
        {
            var eventType = _eventTypes.GetClrTypeFor(@event.Metadata.Type.Id);
            var content = await _eventSerializer.Deserialize(eventType, @event.Content);
            initial = await _reducerInvoker.Invoke(content, initial, @event.Context);
        }

        return initial!;
    }
}
