// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Threading.Channels;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Queries;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Coalesces status notifications into serial claims, recovering a lost observation stream.
/// </summary>
/// <param name="eventStore">The namespace-bound store.</param>
/// <param name="trigger">The trigger identity.</param>
/// <param name="attempt">The attempt runner.</param>
/// <param name="timeProvider">The reconnection clock.</param>
/// <param name="logger">The worker logger.</param>
/// <param name="deliveryScope">Optional lifecycle delivery boundary.</param>
internal sealed class OccurrenceWorker(IEventStore eventStore, TriggerId trigger, OccurrenceAttempt attempt, TimeProvider timeProvider, ILogger logger, OccurrenceDeliveryScope? deliveryScope = null)
{
    /// <summary>
    /// Delivers until its owning connection session ends.
    /// </summary>
    /// <param name="cancellationToken">Session cancellation.</param>
    /// <returns>The worker lifetime.</returns>
    internal async Task Run(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                OccurrenceSupport.Require(eventStore.Connection);
                await Observe(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.OccurrenceWorkerFailed(exception, trigger);
            }
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), timeProvider, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    async Task Observe(CancellationToken cancellationToken)
    {
        var service = ((IChronicleServicesAccessor)eventStore.Connection).Services.Occurrences;
        var wake = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });
        var observation = new ObserveOccurrencesRequest
        {
            EventStore = eventStore.Name,
            Namespace = deliveryScope?.Namespace ?? eventStore.Namespace,
            ApplicationScope = deliveryScope?.ApplicationScope ?? false,
            Trigger = trigger
        };
        using var subscription = service.ObserveOccurrences(observation, new CallContext(new CallOptions(cancellationToken: cancellationToken))).Subscribe(
            result =>
        {
            try
            {
                result.EnsureSuccess();
                wake.Writer.TryWrite(true);
            }
            catch (Exception exception)
            {
                wake.Writer.TryComplete(exception);
            }
            },
            exception => wake.Writer.TryComplete(exception),
            () => wake.Writer.TryComplete());

        // Also claim on attachment: a snapshot or reconnect must not require a new ingress notification.
        wake.Writer.TryWrite(true);
        while (await wake.Reader.WaitToReadAsync(cancellationToken))
        {
            wake.Reader.TryRead(out _);
            while (!cancellationToken.IsCancellationRequested)
            {
                var request = new ClaimOccurrencesRequest
                {
                    EventStore = eventStore.Name,
                    Namespace = deliveryScope?.Namespace ?? eventStore.Namespace,
                    ApplicationScope = deliveryScope?.ApplicationScope ?? false,
                    Trigger = trigger,
                    WorkerId = eventStore.Connection.Lifecycle.ConnectionId.ToString(),
                    LeaseSeconds = OccurrenceAttempt.LeaseSeconds,
                    Max = 1
                };
                var claims = await service.ClaimOccurrences(request, new CallContext(new CallOptions(cancellationToken: cancellationToken))).EnsureSuccess();
                var batch = claims.Claims.ToArray();
                if (batch.Length == 0) break;
                foreach (var claim in batch)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await attempt.Run(claim, cancellationToken);
                }
            }
        }
    }
}
