// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerHandler"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerHandler"/> class.
/// </remarks>
/// <param name="reducerId">The identifier of the reducer.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the reducer is for.</param>
/// <param name="invoker">The actual invoker.</param>
/// <param name="eventSerializer">The event serializer to use.</param>
/// <param name="isActive">Whether or not reducer should be actively running on the Kernel.</param>
public class ReducerHandler(
    ReducerId reducerId,
    EventSequenceId eventSequenceId,
    IReducerInvoker invoker,
    IEventSerializer eventSerializer,
    bool isActive) : IReducerHandler
{
    /// <inheritdoc/>
    public ReducerId Id { get; } = reducerId;

    /// <inheritdoc/>
    public EventSequenceId EventSequenceId { get; } = eventSequenceId;

    /// <inheritdoc/>
    public IEnumerable<EventType> EventTypes => invoker.EventTypes;

    /// <inheritdoc/>
    public Type ReadModelType => invoker.ReadModelType;

    /// <inheritdoc/>
    public bool IsActive { get; } = isActive;

    /// <inheritdoc/>
    public IReducerInvoker Invoker => invoker;

    /// <inheritdoc/>
    public async Task<InternalReduceResult> OnNext(IEnumerable<AppendedEvent> events, object? initial)
    {
        var tasks = events.Select(async @event =>
        {
            var content = await eventSerializer.Deserialize(@event);
            return new EventAndContext(content, @event.Context);
        });
        var eventAndContexts = await Task.WhenAll(tasks.ToArray()!);
        return await invoker.Invoke(eventAndContexts, initial);
    }
}
