// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

public partial class HandleEventsForObserver
{
    static bool CanContinueBucket(ReplayBucket? bucket, ObserverSubscriberResult result, bool page) =>
        bucket is not null && result.State == ObserverSubscriberState.Failed &&
        (!page || (result.FailedPartition is not null && result.FailedAt.IsActualValue));

    static void ValidateBucket(ReplayBucket? bucket)
    {
        if (bucket is { HashVersion: not 1 })
        {
            throw new NotSupportedException($"Replay bucket hash version {bucket.HashVersion} is not supported.");
        }
    }

    static AppendedEvent[] FilterBucketEvents(ReplayBucket? bucket, AppendedEvent[] events) => bucket is null
        ? events
        : events.Where(@event => ReplayBuckets.For(@event.Context.EventSourceId, bucket.Count) == bucket).ToArray();

    async Task<(JobStepResult? Result, EventSequenceNumber LastSuccessful)> FailureResult(
        ReplayBucket? bucket,
        Key partition,
        ObserverSubscriberResult result,
        (EventSequenceNumber LastSuccessful, bool Page) context)
    {
        if (CanContinueBucket(bucket, result, context.Page))
        {
            return (await RecordBucketFailure(partition) ? null : ClosedResult(), context.LastSuccessful);
        }

        return (JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            CreateResult(_lastDurableCheckpoint), result.ExceptionMessages, result.ExceptionStackTrace)), context.LastSuccessful);
    }

    void RestoreBucketFailures(HandleEventsForObserverState state) =>
        _bucketFailedPartitions = state.Bucket is null ? [] : [.. state.BucketFailedPartitions];

    async Task<JobStepResult?> DispatchFailureResult(ReplayBucket? bucket, Key partition, IEnumerable<string> exceptionMessages)
    {
        if (bucket is not null) return await RecordBucketFailure(partition) ? null : ClosedResult();
        return JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            CreateResult(_lastDurableCheckpoint),
            TransientTimeouts.ShortenForStep(exceptionMessages),
            string.Empty));
    }

    async Task<bool> RecordBucketFailure(Key partition)
    {
        var recorded = await TryInvokeOnActivation(async () =>
        {
            State.BucketFailedPartitions = [.. _bucketFailedPartitions, partition];
            var persisted = await WriteStateAsync();
            persisted.RethrowError();
        });
        if (!recorded.IsSuccess) return false;
        _bucketFailedPartitions.Add(partition);
        return true;
    }

    async Task<(JobStepResult? Result, EventSequenceNumber LastSuccessful, bool Handled, AppendedEvent[] Remaining)> HandlePage(
        (HandleEventsForObserverState State, TimeSpan Timeout) context,
        AppendedEvent[] events,
        EventSequenceNumber lastSuccessful,
        CancellationToken cancellationToken)
    {
        var remaining = events.Where(@event => !_bucketFailedPartitions.Contains(@event.Context.EventSourceId)).ToArray();
        while (remaining.Length > 0)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return (await CancelledPageResult(), lastSuccessful, true, []);
            }

            var dispatched = false;
            var pageResult = await TryHandleEvents(ObserverSubscriberKey.AllPartitions, remaining, context.Timeout, page: true, onPageDispatch: () => dispatched = true);
            if (dispatched && pageResult.TryGetException(out var exception))
            {
                // No authoritative partition or prefix: never redispatch a page that may still be applying writes.
                return (await HandlePageDispatchException(remaining[0], exception), lastSuccessful, true, []);
            }

            if (!pageResult.TryGetResult(out var handled)) return (null, lastSuccessful, false, remaining);
            var (result, position) = await HandleSubscriberResult(
                context.State, remaining[0].Context.EventSourceId, handled.Result, handled.HandledEvents, lastSuccessful, cancellationToken, page: true);
            lastSuccessful = position;
            if (result is not null) return (result, lastSuccessful, true, []);
            if (!await FlushReplayReports()) return (ClosedResult(), lastSuccessful, true, []);
            remaining = RemainingAfterBucketFailure(context.State.Bucket, handled.Result, remaining);
        }

        return (null, lastSuccessful, true, []);
    }

    async Task<JobStepResult> CancelledPageResult() => await FlushReplayReports()
        ? JobStepResult.Failed(PerformJobStepError.CancelledWithPartialResult(CreateResult(_lastDurableCheckpoint)))
        : ClosedResult();

    AppendedEvent[] RemainingAfterBucketFailure(ReplayBucket? bucket, ObserverSubscriberResult result, AppendedEvent[] events) =>
        CanContinueBucket(bucket, result, page: true)
            ? events.Where(@event => @event.Context.SequenceNumber > result.FailedAt && !_bucketFailedPartitions.Contains(@event.Context.EventSourceId)).ToArray()
            : [];
}
