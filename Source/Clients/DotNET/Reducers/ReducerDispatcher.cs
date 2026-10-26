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
            await HandleReplayNotification(handler, operation.ReplayState, operation.Partition);
            return;
        }

        using var span = activitySource.Handle(
            new(eventStore.Name, @namespace, handler.EventSequenceId, handler.Id),
            SpanNames);

        var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;
        var exceptionMessages = Enumerable.Empty<string>();
        var exceptionStackTrace = string.Empty;
        var state = ObservationState.Success;
        string? modelState = null;

        var appendedEvents = operation.Events.Select(@event =>
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
            return new AppendedEvent(
                context,
                content);
        }).ToList();

        try
        {
            await using var serviceProviderScope = serviceProvider.CreateAsyncScope();
            identityProvider.SetCurrentIdentity(Identity.System);
            var initialState = operation.InitialState is null ? null : JsonSerializer.Deserialize(operation.InitialState, handler.ReadModelType, jsonSerializerOptions);
            var reduceResult = await handler.OnNext(appendedEvents, initialState, serviceProviderScope.ServiceProvider);

            lastSuccessfullyObservedEvent = reduceResult.LastSuccessfullyObservedEvent;
            if (reduceResult.IsSuccess)
            {
                modelState = reduceResult.ReadModelState is null ?
                    null :
                    JsonSerializer.Serialize(reduceResult.ReadModelState, jsonSerializerOptions);
            }
            else
            {
                span?.Activity?.RecordFailure(reduceResult.ExceptionType ?? "reducer_error");
                exceptionMessages = reduceResult.ErrorMessages;
                exceptionStackTrace = reduceResult.StackTrace;
                state = ObservationState.Failed;
                logger.ReducerFailed(
                    handler.Id,
                    appendedEvents[0].Context.SequenceNumber,
                    appendedEvents[^1].Context.SequenceNumber,
                    string.Join(Environment.NewLine, reduceResult.ErrorMessages));
            }
        }
        catch (Exception ex)
        {
            span?.Activity?.RecordFailure(ex);
            logger.ErrorWhileHandlingEvents(ex, appendedEvents[0].Context.SequenceNumber, appendedEvents[^1].Context.SequenceNumber, handler.Id);
            exceptionMessages = ex.GetAllMessages();
            exceptionStackTrace = ex.StackTrace ?? string.Empty;
            state = ObservationState.Failed;
        }

        var result = new ReducerResult
        {
            Partition = operation.Partition,
            DeliveryId = operation.DeliveryId,
            ReadModelState = modelState,
            State = state,
            LastSuccessfulObservation = lastSuccessfullyObservedEvent,
            ExceptionMessages = exceptionMessages.ToList(),
            ExceptionStackTrace = exceptionStackTrace
        };
        publish(result);
    }

    async Task HandleReplayNotification(IReducerHandler handler, ReplayState replayState, string partition)
    {
        if (handler.Invoker is SelfReducerInvoker self)
        {
            await self.ReplayHooks.Invoke(serviceProvider, replayState, partition);
            return;
        }

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
