// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Receivers;

/// <summary>
/// Decodes and dispatches deliveries through the requested event store's existing dispatchers.
/// </summary>
/// <param name="client">The Chronicle client.</param>
/// <param name="decoder">The delivery decoder.</param>
/// <param name="logger">The receiver logger.</param>
/// <param name="targets">The configured target validation.</param>
/// <remarks>
/// There is no non-creating namespace lookup. Only the configured namespace resolver's current namespace is
/// accepted, before leaving the request execution context. Unverified deliveries are always refused.
/// </remarks>
internal sealed class ChronicleReceiver(IChronicleClient client, IDeliveryDecoder decoder, ILogger<ChronicleReceiver> logger, DeliveryTargetValidation targets) : IChronicleReceiver
{
    /// <inheritdoc/>
    public async Task<DeliveryOutcome> Receive(DeliveryRequest request, CancellationToken cancellationToken)
    {
        if (!request.SignatureVerified)
        {
            return DeliveryOutcome.Cancelled(string.Empty);
        }

        Delivery? delivery = null;
        try
        {
            delivery = await DecodeVerifiedDelivery(request, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return DeliveryOutcome.Cancelled(delivery.Partition);
            }

            return await DispatchOutsideRequest(delivery, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            logger.Rejected(DeliveryStatus.Malformed);
            return DeliveryOutcome.Rejected(DeliveryStatus.Malformed, exception.Message);
        }
        catch (ArgumentOutOfRangeException exception) when (IsBodyLimit(exception, delivery))
        {
            logger.Rejected(DeliveryStatus.TooLarge);
            return DeliveryOutcome.Rejected(DeliveryStatus.TooLarge, exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.Rejected(DeliveryStatus.Cancelled);
            return DeliveryOutcome.Cancelled(delivery?.Partition ?? string.Empty);
        }
        catch (Exception exception) when (IsUnknownObserver(exception))
        {
            logger.Rejected(DeliveryStatus.UnknownObserver);
            return DeliveryOutcome.Rejected(DeliveryStatus.UnknownObserver, "Unknown observer.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Failed();
            return new(DeliveryStatus.Failed, delivery?.Partition ?? string.Empty, EventSequenceNumber.Unavailable, [.. exception.GetAllMessages()], exception.StackTrace ?? string.Empty, null);
        }
    }

    static bool IsBodyLimit(ArgumentOutOfRangeException exception, Delivery? delivery) => delivery is null && exception.ParamName == "source";

    static bool IsUnknownObserver(Exception exception) => exception is UnknownReactorId or ReducerDoesNotExist;

    /// <summary>
    /// Dispatches a reactor batch without opening a stream.
    /// </summary>
    /// <param name="reactors">The store's reactor delivery entry point.</param>
    /// <param name="delivery">The decoded delivery.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The outcome.</returns>
    static async Task<DeliveryOutcome> DispatchReactor(IReceiveReactorDeliveries reactors, Delivery delivery, CancellationToken cancellationToken)
    {
        var result = await reactors.Deliver(
            new ReactorId(delivery.Target.ObserverId),
            new EventsToObserve
            {
                Partition = delivery.Partition,
                Events = [.. delivery.Events]
            },
            cancellationToken);
        return result is null ? DeliveryOutcome.Cancelled(delivery.Partition) : DeliveryOutcome.From(result);
    }

    /// <summary>
    /// Dispatches a reducer operation without opening a stream.
    /// </summary>
    /// <param name="reducers">The store's reducer delivery entry point.</param>
    /// <param name="delivery">The decoded delivery.</param>
    /// <returns>The outcome.</returns>
    static async Task<DeliveryOutcome> DispatchReducer(IReceiveReducerDeliveries reducers, Delivery delivery)
    {
        var result = await reducers.Deliver(new ReducerId(delivery.Target.ObserverId), new ReduceOperationMessage
        {
            Partition = delivery.Partition,
            Events = [.. delivery.Events],
            InitialState = delivery.InitialState
        });
        return result is null ? DeliveryOutcome.Cancelled(delivery.Partition) : DeliveryOutcome.From(result);
    }

    async Task<Delivery> DecodeVerifiedDelivery(DeliveryRequest request, CancellationToken cancellationToken)
    {
        var target = targets.Validate(request.Target) ?? throw new InvalidDataException("Unknown delivery target.");
        var delivery = await decoder.Decode(request, cancellationToken);
        if (delivery.Target != target)
        {
            throw new InvalidDataException("Delivery target does not match the request.");
        }

        return delivery with { Target = target };
    }

    Task<DeliveryOutcome> DispatchOutsideRequest(Delivery delivery, CancellationToken cancellationToken)
    {
        var parent = Activity.Current;
        Task<DeliveryOutcome> task;
        using (ExecutionContext.SuppressFlow())
        {
            task = Task.Run(
                () =>
                {
                    Activity.Current = parent;
                    return Dispatch(delivery, cancellationToken);
                },
                CancellationToken.None);
        }

        return task;
    }

    /// <summary>
    /// Dispatches outside the caller's ambient request, identity and unit of work.
    /// </summary>
    /// <param name="delivery">The decoded delivery.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The delivery outcome.</returns>
    async Task<DeliveryOutcome> Dispatch(Delivery delivery, CancellationToken cancellationToken)
    {
        var target = delivery.Target;
        logger.Receiving(target.ObserverType, target.ObserverId, target.EventStore.Value, target.Namespace.Value, delivery.Partition, delivery.Events.Count);
        var store = await client.GetEventStore(target.EventStore, target.Namespace);
        var outcome = target.ObserverType switch
        {
            Observation.ObserverType.Reactor when store.Reactors is IReceiveReactorDeliveries reactors => await DispatchReactor(reactors, delivery, cancellationToken),
            Observation.ObserverType.Reducer when store.Reducers is IReceiveReducerDeliveries reducers => await DispatchReducer(reducers, delivery),
            _ => DeliveryOutcome.Rejected(DeliveryStatus.UnknownObserver, "Unknown observer.")
        };
        logger.Completed(target.ObserverId, outcome.Status);
        return outcome;
    }
}
