// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a job for retrying a failed partition.
/// </summary>
/// <param name="jsonSerializerOptions">The serializer options used for JSON serialization.</param>
/// <param name="storage">The <see cref="IStorage"/> used to confirm there is nothing left to handle before clearing a failure.</param>
/// <param name="logger">The logger.</param>
public class RetryFailedPartition(
    JsonSerializerOptions jsonSerializerOptions,
    IStorage storage,
    ILogger<RetryFailedPartition> logger) : Job<RetryFailedPartitionRequest, JobStateWithLastHandledEvent>, IRetryFailedPartition
{
    /// <inheritdoc/>
    protected override async Task OnAllStepsCompleted()
    {
        using var scope = logger.BeginJobScope(JobId, JobKey);
        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);

        if (!State.LastHandledEventSequenceNumber.IsActualValue)
        {
            logger.NoEventsWereHandled(nameof(RetryFailedPartition));

            if (!State.SucceededWithoutHandlingAnyEvents)
            {
                // The step ran but the subscriber failed every event — the partition is still
                // legitimately failed. Do not clear it; the next scheduled retry will try again.
                return;
            }

            // The step succeeded having read nothing. Clearing the failure here advances the observer past
            // the failed event without the handler ever running, so it is only correct when there genuinely
            // is no event left to handle — otherwise recovery silently discards the missed side effect and
            // reports the observer healthy. Confirm it against the event sequence before clearing.
            if (await HasEventsLeftToHandle())
            {
                logger.NotClearingFailedPartitionWithEventsLeftToHandle(Request.Key, Request.FromSequenceNumber);
                return;
            }

            logger.ClearingFailedPartitionWithNothingLeftToHandle(Request.Key, Request.FromSequenceNumber);
            await observer.FailedPartitionRecovered(Request.Key, Request.FromSequenceNumber);
            return;
        }

        if (!State.HandledAllEvents)
        {
            logger.NotAllEventsWereHandled(nameof(RetryFailedPartition), State.LastHandledEventSequenceNumber);
            await observer.FailedPartitionPartiallyRecovered(Request.Key, State.LastHandledEventSequenceNumber);
            return;
        }

        await observer.FailedPartitionRecovered(Request.Key, State.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"{Request.ObserverKey.ObserverId}-{Request.Key}";

    /// <inheritdoc/>
    protected override Task<bool> CanResume()
    {
        if (State.Request is null)
        {
            return Task.FromResult(false);
        }

        var observer = GrainFactory.GetGrain<IObserver>(Request.ObserverKey);
        return observer.IsSubscribed();
    }

    /// <inheritdoc/>
    protected override Task OnStepCompletedOrStopped(JobStepId jobStepId, JobStepResult result)
    {
        State.HandleResult(result, jsonSerializerOptions);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(RetryFailedPartitionRequest request)
    {
        var steps = new[]
        {
            CreateStep<IHandleEventsForPartition>(
                new HandleEventsForPartitionArguments(
                    request.ObserverKey,
                    request.ObserverType,
                    request.Key,
                    request.FromSequenceNumber,
                    EventSequenceNumber.Max,
                    EventObservationState.None,
                    request.EventTypes))
        }.ToImmutableList();

        return Task.FromResult<IImmutableList<JobStepDetails>>(steps);
    }

    /// <summary>
    /// Check whether the event sequence still holds an event the failed partition has not handled.
    /// </summary>
    /// <returns>True when there is at least one event left to handle, false when there is nothing left.</returns>
    /// <remarks>
    /// The answer decides whether a step that read nothing is evidence of a stale failure record. When the
    /// event sequence cannot be reached the honest answer is "assume there is" — leaving the partition failed
    /// costs another retry, while clearing it loses the work for good.
    /// </remarks>
    async Task<bool> HasEventsLeftToHandle()
    {
        try
        {
            var eventSequenceStorage = storage
                .GetEventStore(Request.ObserverKey.EventStore)
                .GetNamespace(Request.ObserverKey.Namespace)
                .GetEventSequence(Request.ObserverKey.EventSequenceId);

            var eventTypes = Request.EventTypes?.ToArray() ?? [];
            var nextSequenceNumber = await eventSequenceStorage.GetNextSequenceNumberGreaterOrEqualThan(
                Request.FromSequenceNumber,
                eventTypes.Length == 0 ? null : eventTypes,
                Request.Key);

            return nextSequenceNumber.IsActualValue;
        }
        catch (Exception ex)
        {
            logger.FailedCheckingForEventsLeftToHandle(ex, Request.Key, Request.FromSequenceNumber);
            return true;
        }
    }
}
