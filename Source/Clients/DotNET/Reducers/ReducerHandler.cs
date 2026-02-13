// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerHandler"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReducerHandler"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> the reducers belong to.</param>
/// <param name="reducerId">The identifier of the reducer.</param>
/// <param name="reducerType">The type of the reducer.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the reducer is for.</param>
/// <param name="invoker">The actual invoker.</param>
/// <param name="isActive">Whether or not reducer should be actively running on the Kernel.</param>
/// <param name="reducerObservers"><see cref="IReducerObservers"/> for notifying observers of changes.</param>
public class ReducerHandler(
    IEventStore eventStore,
    ReducerId reducerId,
    Type reducerType,
    EventSequenceId eventSequenceId,
    IReducerInvoker invoker,
    bool isActive,
    IReducerObservers reducerObservers) : IReducerHandler, IDisposable
{
    readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <inheritdoc/>
    public ReducerId Id { get; } = reducerId;

    /// <inheritdoc/>
    public Type ReducerType { get; } = reducerType;

    /// <inheritdoc/>
    public EventSequenceId EventSequenceId { get; } = eventSequenceId;

    /// <inheritdoc/>
    public IEnumerable<EventType> EventTypes => invoker.EventTypes;

    /// <inheritdoc/>
    public Type ReadModelType => invoker.ReadModelType;

    /// <inheritdoc/>
    public ReadModelContainerName ContainerName => invoker.ContainerName;

    /// <inheritdoc/>
    public bool IsActive { get; } = isActive;

    /// <inheritdoc/>
    public IReducerInvoker Invoker => invoker;

    /// <inheritdoc/>
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    /// <inheritdoc/>
    public async Task<ReduceResult> OnNext(IEnumerable<AppendedEvent> events, object? initial, IServiceProvider serviceProvider)
    {
        var eventAndContexts = events.Select(@event => new EventAndContext(@event.Content, @event.Context));
        var result = await invoker.Invoke(serviceProvider, eventAndContexts, initial);

        var modelKey = new ReadModelKey(events.First().Context.EventSourceId.Value);
        var @namespace = eventStore.Namespace;
        var removed = result.ReadModelState == null;

        var notifyMethod = typeof(IReducerObservers).GetMethod(nameof(IReducerObservers.NotifyChange))!
            .MakeGenericMethod(ReadModelType);
        notifyMethod.Invoke(reducerObservers, [@namespace, modelKey, result.ReadModelState, removed]);

        return result;
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
