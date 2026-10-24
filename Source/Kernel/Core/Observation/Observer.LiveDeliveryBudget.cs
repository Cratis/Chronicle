// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    static int _liveTimeoutClampLogged;

    /// <summary>
    /// Bounds a live subscriber wait to leave time for persisting the observer's result before Orleans times out.
    /// </summary>
    /// <param name="subscriberTimeout">The configured subscriber timeout; zero or less waits indefinitely.</param>
    /// <param name="responseTimeout">The effective Orleans response timeout.</param>
    /// <returns>The bounded live wait.</returns>
    internal static TimeSpan ClampLiveSubscriberTimeout(TimeSpan subscriberTimeout, TimeSpan responseTimeout)
    {
        // Keep the bound positive even when response timeouts are configured below the usual five-second headroom.
        var liveLimit = responseTimeout - TimeSpan.FromSeconds(5);
        if (liveLimit <= TimeSpan.Zero)
        {
            liveLimit = TimeSpan.FromTicks(1);
        }

        return subscriberTimeout <= TimeSpan.Zero || subscriberTimeout > liveLimit ? liveLimit : subscriberTimeout;
    }

    [LoggerMessage(LogLevel.Information, "Deferring live batch of {EventCount} events for partition {Partition} to catch-up")]
    static partial void LogLiveBatchDeferred(ILogger logger, int eventCount, Key partition);

    [LoggerMessage(LogLevel.Warning, "Could not hand live batch for partition {Partition} over to catch-up ({Error}); delivering it live")]
    static partial void LogLiveBatchHandOverRefused(ILogger logger, Key partition, StartJobError error);

    [LoggerMessage(LogLevel.Warning, "Clamping live subscriber timeout from {ConfiguredTimeout} to {LiveTimeout} to leave Orleans response headroom")]
    static partial void LogLiveTimeoutClamped(ILogger logger, TimeSpan configuredTimeout, TimeSpan liveTimeout);

    async Task<bool> ExceedsLiveBudget(int eventCount)
    {
        var configuration = await configurationProvider.GetFor(_observerKey);
        return configuration.LiveDeliveryBudget > 0 && eventCount > configuration.LiveDeliveryBudget;
    }

    /// <summary>
    /// Hands a live batch that exceeds the live delivery budget over to a partition catch-up job.
    /// </summary>
    /// <param name="partition">The partition the batch belongs to.</param>
    /// <param name="events">The events of the batch.</param>
    /// <returns>True if catch-up owns the batch; false if it must be delivered live after all.</returns>
    async Task<bool> HandOverLargeLiveBatchToCatchUp(Key partition, AppendedEvent[] events)
    {
        if (IsRetired || _removed || State.CatchingUpPartitions.Contains(partition))
        {
            return true;
        }

        // From the moment the partition is marked as catching up until its job is started, the handover owns
        // catch-up. The watchdog interleaves with this call and must not rescue in that window.
        _catchUpHandoversInFlight++;
        _catchUpHandoversSettled ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
        _catchUpOwnershipEpoch++;
        var sequenceNumber = events[0].Context.SequenceNumber;
        try
        {
            LogLiveBatchDeferred(logger, events.Length, partition);
            State.CatchingUpPartitions.Add(partition);
            await WriteStateAsync();
            var startResult = await _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(
                new(_observerKey, Definition.Type, partition, sequenceNumber, Definition.EventTypes) { ReaderScope = ReaderScope });
            if (startResult is not null && startResult.TryGetResult(out _))
            {
                Skips.JobStarted(partition);
                return true;
            }

            // A refused start leaves no job to deliver the batch, so it is delivered live instead.
            var error = startResult is not null && startResult.TryGetError(out var startError) ? startError : default;
            LogLiveBatchHandOverRefused(logger, partition, error);
            State.CatchingUpPartitions.Remove(partition);
            await WriteStateAsync();
            return false;
        }
        catch (Exception exception)
        {
            State.CatchingUpPartitions.Remove(partition);
            await PartitionFailed(
                partition,
                sequenceNumber,
                exception.GetAllMessages(),
                exception.StackTrace ?? string.Empty);
            return true;
        }
        finally
        {
            if (--_catchUpHandoversInFlight == 0 && _catchUpHandoversSettled is { } settled)
            {
                _catchUpHandoversSettled = null;
                settled.SetResult();
            }
        }
    }

    TimeSpan GetLiveSubscriberTimeout(TimeSpan subscriberTimeout)
    {
        var messaging = ServiceProvider.GetService<IOptions<SiloMessagingOptions>>()?.Value ?? new SiloMessagingOptions();
        var responseTimeout = messaging.ResponseTimeout > messaging.SystemResponseTimeout ? messaging.ResponseTimeout : messaging.SystemResponseTimeout;
        var liveTimeout = ClampLiveSubscriberTimeout(subscriberTimeout, responseTimeout);
        if (liveTimeout != subscriberTimeout && Interlocked.Exchange(ref _liveTimeoutClampLogged, 1) == 0)
        {
            LogLiveTimeoutClamped(logger, subscriberTimeout, liveTimeout);
        }

        return liveTimeout;
    }
}
