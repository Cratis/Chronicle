// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerHandler"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerHandler"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> the reducers belong to.</param>
/// <param name="reducerId">The identifier of the reducer.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the reducer is for.</param>
/// <param name="invoker">The actual invoker.</param>
/// <param name="eventSerializer">The event serializer to use.</param>
/// <param name="isActive">Whether or not reducer should be actively running on the Kernel.</param>
public class ReducerHandler(
    IEventStore eventStore,
    ReducerId reducerId,
    EventSequenceId eventSequenceId,
    IReducerInvoker invoker,
    IEventSerializer eventSerializer,
    bool isActive) : IReducerHandler, IDisposable
{
    readonly CancellationTokenSource _cancellationTokenSource = new();

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
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    /// <inheritdoc/>
    public async Task<ReduceResult> OnNext(IEnumerable<AppendedEvent> events, object? initial, IServiceProvider serviceProvider)
    {
        var tasks = events.Select(async @event =>
        {
            var content = await eventSerializer.Deserialize(@event);
            return new EventAndContext(content, @event.Context);
        });
        var eventAndContexts = await Task.WhenAll(tasks.ToArray()!);
        return await invoker.Invoke(serviceProvider, eventAndContexts, initial);
    }

    /// <inheritdoc/>
    public void Disconnect() => _cancellationTokenSource.Cancel();

    /// <inheritdoc/>
    public async Task<ReducerState> GetState()
    {
        var request = new Contracts.Observation.GetObserverInformationRequest
        {
            ObserverId = Id,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = EventSequenceId
        };
        var servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        var state = await servicesAccessor.Services.Observers.GetObserverInformation(request);
        return new ReducerState(
            state.RunningState.ToClient(),
            state.IsSubscribed,
            state.NextEventSequenceNumber,
            state.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> GetFailedPartitions() =>
        eventStore.FailedPartitions.GetFailedPartitionsFor(Id);

    /// <inheritdoc/>
    public void Dispose() => _cancellationTokenSource.Dispose();
}
