// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Observation;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Dispatches delivered events to a reducer, independent of the transport that delivered them.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> the reducers belong to.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> to get instances of types.</param>
/// <param name="artifactActivator"><see cref="IClientArtifactsActivator"/> for creating artifact instances.</param>
/// <param name="eventTypes">Registered <see cref="IEventTypes"/>.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for JSON serialization.</param>
/// <param name="identityProvider"><see cref="IIdentityProvider"/> for managing identity context.</param>
/// <param name="activitySource"><see cref="IActivitySource{T}"/> for tracing reducer event handling.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
internal sealed class ReducerDispatcher(
    IEventStore eventStore,
    IServiceProvider serviceProvider,
    IClientArtifactsActivator artifactActivator,
    IEventTypes eventTypes,
    JsonSerializerOptions jsonSerializerOptions,
    IIdentityProvider identityProvider,
    IActivitySource<Reducers> activitySource,
    ILogger<Reducers> logger)
{
    readonly ReducerStateCache _stateCache = new();

    /// <summary>Gets or sets the client span naming convention.</summary>
    internal ClientSpanNames SpanNames { get; set; }

    /// <summary>
    /// Dispatches a batch of events, or a replay notification, to a reducer.
    /// </summary>
    /// <param name="handler">The <see cref="IReducerHandler"/> to dispatch to.</param>
    /// <param name="operation">The <see cref="ReduceOperationMessage"/> to dispatch.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the events are delivered for.</param>
    /// <param name="publish">Callback for publishing the <see cref="ReducerResult"/> of the batch.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Dispatch(
        IReducerHandler handler,
        ReduceOperationMessage operation,
        EventStoreNamespaceName @namespace,
        Action<ReducerResult> publish)
    {
        if (operation.ReplayState != ReplayState.None)
        {
            if (string.IsNullOrEmpty(operation.Partition))
            {
                _stateCache.ForgetAll(handler.Id);
            }
            else
            {
                _stateCache.Forget(handler.Id, operation.Partition);
            }

            await HandleReplayNotification(handler, @namespace, operation.ReplayState, operation.Partition);
            return;
        }

        using var span = activitySource.Handle(
            new(eventStore.Name, @namespace, handler.EventSequenceId, handler.Id),
            SpanNames);

        var partitions = operation.PartitionsOf();
        var batchContext = CreateBatchContext();
        var outcomes = new PartitionOutcome[partitions.Count];
        await Parallel.ForEachAsync(
            Enumerable.Range(0, partitions.Count),
            new ParallelOptions { MaxDegreeOfParallelism = ObserverBatchSize.For(handler.ReducerType, handler.Invoker is IBatchReducerInvoker { HasBatchMethod: true }).MaxConcurrency },
            async (index, _) => outcomes[index] = await Reduce(partitions[index]));

        publish(operation.Partitions.Count > 0
            ? new ReducerResult
            {
                DeliveryId = operation.DeliveryId,
                Partitions = [.. outcomes.Select(_ => _.ToContract())]
            }
            : new ReducerResult
            {
                Partition = operation.Partition,
                DeliveryId = operation.DeliveryId,
                ReadModelState = outcomes[0].ReadModelState,
                State = outcomes[0].State == ObservationState.Deferred ? ObservationState.Failed : outcomes[0].State,
                LastSuccessfulObservation = outcomes[0].LastSuccessfulObservation,
                ExceptionMessages = [.. outcomes[0].ExceptionMessages],
                ExceptionStackTrace = outcomes[0].ExceptionStackTrace
            });

        async Task<PartitionOutcome> Reduce(ReducerPartition partition)
        {
            var partitionKey = partition.Partition ?? string.Empty;
            var firstEvent = partition.Events.Count > 0 ? (EventSequenceNumber)partition.Events[0].Context.SequenceNumber : EventSequenceNumber.Unavailable;
            var lastEvent = partition.Events.Count > 0 ? (EventSequenceNumber)partition.Events[^1].Context.SequenceNumber : EventSequenceNumber.Unavailable;
            try
            {
                var appendedEvents = partition.Events.Select(ToAppendedEvent).ToList();
                using var delivery = ObserverDeliveryScope.Enter(serviceProvider, DeliveryContextFor(handler, @namespace));
                await using var serviceProviderScope = serviceProvider.CreateAsyncScope();
                identityProvider.SetCurrentIdentity(Identity.System);
                var initialStateJson = partition.InitialState;
                if (partition.InitialStateUnchanged && !_stateCache.TryGet(handler.Id, partitionKey, out initialStateJson))
                {
                    // The kernel believes we hold the state, but we do not (evicted, restarted or never cached). Failing clears
                    // the kernel's record of it, so the redelivery carries the full state.
                    _stateCache.Forget(handler.Id, partitionKey);
                    return PartitionOutcome.Failed(partitionKey, new InvalidOperationException($"The kernel sent the state of partition '{partitionKey}' as unchanged, but reducer '{handler.Id}' has no cached state for it. The state is requested in full on redelivery."));
                }

                var initialState = initialStateJson is null ? null : JsonSerializer.Deserialize(initialStateJson, handler.ReadModelType, jsonSerializerOptions);
                var reduceResult = handler is ReducerHandler reducerHandler
                    ? await reducerHandler.OnNext(appendedEvents, initialState, serviceProviderScope.ServiceProvider, batchContext)
                    : await handler.OnNext(appendedEvents, initialState, serviceProviderScope.ServiceProvider);

                if (reduceResult.RetryAfter is { } retryAfter)
                {
                    _stateCache.Forget(handler.Id, partitionKey);
                    return new(partitionKey, ObservationState.Deferred, EventSequenceNumber.Unavailable, [], string.Empty) { RetryAfter = retryAfter };
                }

                if (reduceResult.IsSuccess)
                {
                    var readModelState = reduceResult.ReadModelState is null ? null : JsonSerializer.Serialize(reduceResult.ReadModelState, jsonSerializerOptions);

                    // Remember exactly what the kernel is told, so an unchanged marker resolves to the state the kernel fingerprinted.
                    _stateCache.Set(handler.Id, partitionKey, readModelState);
                    return new(partitionKey, ObservationState.Success, reduceResult.LastSuccessfullyObservedEvent, [], string.Empty)
                    {
                        ReadModelState = readModelState
                    };
                }

                _stateCache.Forget(handler.Id, partitionKey);

                span?.Activity?.RecordFailure(reduceResult.ExceptionType ?? "reducer_error");
                logger.ReducerFailed(handler.Id, firstEvent, lastEvent, string.Join(Environment.NewLine, reduceResult.ErrorMessages));
                return new(partitionKey, ObservationState.Failed, reduceResult.LastSuccessfullyObservedEvent, reduceResult.ErrorMessages, reduceResult.StackTrace);
            }
            catch (Exception ex)
            {
                _stateCache.Forget(handler.Id, partitionKey);
                span?.Activity?.RecordFailure(ex);
                logger.ErrorWhileHandlingEvents(ex, firstEvent, lastEvent, handler.Id);
                return PartitionOutcome.Failed(partitionKey, ex);
            }
        }

        BatchContext CreateBatchContext()
        {
            var allEvents = partitions.SelectMany(_ => _.Events).Select(_ => _.Context.SequenceNumber).ToArray();
            return new(
                handler.Id.Value,
                eventStore.Name,
                @namespace,
                handler.EventSequenceId,
                operation.Origin.ToClient(),
                [.. partitions.Select(_ => new EventSourceId(_.Partition ?? string.Empty))],
                allEvents.Length,
                allEvents.Length > 0 ? allEvents.Min() : EventSequenceNumber.Unavailable,
                allEvents.Length > 0 ? allEvents.Max() : EventSequenceNumber.Unavailable,
                operation.DeliverWithinMilliseconds > 0 ? DateTimeOffset.UtcNow.AddMilliseconds(operation.DeliverWithinMilliseconds) : null,
                handler.CancellationToken)
            {
                CanDefer = operation.Partitions.Count > 0
            };
        }

        AppendedEvent ToAppendedEvent(Contracts.Events.AppendedEvent @event)
        {
            var context = @event.Context.ToClient();
            var handlerEventType = handler.EventTypes.FirstOrDefault(et => et.Id == context.EventType.Id);
            var targetGeneration = handlerEventType?.Generation ?? context.EventType.Generation;
            var eventType = eventTypes.GetClrTypeFor(context.EventType.Id, targetGeneration);

            string contentJson;
            if (targetGeneration != context.EventType.Generation &&
                @event.GenerationalContent.TryGetValue((int)targetGeneration.Value, out var genContent))
            {
                contentJson = genContent;
                context = context with { EventType = context.EventType with { Generation = targetGeneration } };
            }
            else
            {
                contentJson = @event.Content;
            }

            var content = JsonSerializer.Deserialize(contentJson, eventType, jsonSerializerOptions)!;
            return new AppendedEvent(context, content);
        }
    }

    ObserverDeliveryContext DeliveryContextFor(IReducerHandler handler, EventStoreNamespaceName @namespace) =>
        new(eventStore.Name, @namespace, new(handler.Id.Value), ObserverDeliveryKind.Reducer, handler.ReducerType);

    async Task HandleReplayNotification(IReducerHandler handler, EventStoreNamespaceName @namespace, ReplayState replayState, string partition)
    {
        if (handler.Invoker is SelfReducerInvoker self)
        {
            await self.ReplayHooks.Invoke(serviceProvider, DeliveryContextFor(handler, @namespace), replayState, partition);
            return;
        }

        ObserverDeliveryScope delivery;
        try
        {
            delivery = ObserverDeliveryScope.Enter(serviceProvider, DeliveryContextFor(handler, @namespace));
        }
        catch (Exception enterException)
        {
            logger.FailedEnteringDeliveryScopeForReducerReplayNotification(enterException, handler.Id, replayState);
            return;
        }

        using var deliveryScope = delivery;
        await using var serviceProviderScope = serviceProvider.CreateAsyncScope();
        var activatedReducerResult = artifactActivator.Activate(serviceProviderScope.ServiceProvider, handler.ReducerType);
        if (activatedReducerResult.TryGetException(out var ex))
        {
            logger.FailedActivatingReducerForReplayNotification(ex, handler.Id, replayState);
            return;
        }

        await using var activatedReducer = activatedReducerResult.AsT0;
        switch (replayState)
        {
            case ReplayState.BeginReplay when activatedReducer.Instance is ICanBeNotifiedWhenReplay notifiable:
                await notifiable.BeginReplay();
                break;

            case ReplayState.EndReplay when activatedReducer.Instance is ICanBeNotifiedWhenReplay notifiable:
                await notifiable.EndReplay();
                break;

            case ReplayState.BeginReplayPartition when activatedReducer.Instance is ICanBeNotifiedWhenPartitionReplayed notifiable:
                await notifiable.BeginReplayPartition(partition);
                break;

            case ReplayState.EndReplayPartition when activatedReducer.Instance is ICanBeNotifiedWhenPartitionReplayed notifiable:
                await notifiable.EndReplayPartition(partition);
                break;
        }
    }
}
