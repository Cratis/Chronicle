// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <inheritdoc/>
    public Task PartitionFailed(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        FailureKind kind = FailureKind.Unknown,
        FailedPartitionId? episodeId = null,
        Guid? deferralId = null,
        WebhookDeliveryFailurePolicy failurePolicy = WebhookDeliveryFailurePolicy.Retry) =>
        RecordPartitionFailure(new(partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind, episodeId, deferralId, new(failurePolicy)));

    /// <inheritdoc/>
    public Task PartitionFailed(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        FailureKind kind,
        FailedPartitionId? episodeId,
        Guid? deferralId,
        PartitionFailureContext context) =>
        RecordPartitionFailure(new(partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind, episodeId, deferralId, context));

    /// <inheritdoc/>
    public Task PartitionFailedWithDeliveryFence(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        FailureKind kind,
        PartitionFailureContext context) =>
        RecordPartitionFailure(new(partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind, null, null, context));

    /// <inheritdoc/>
    public Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber) =>
        FailedPartitionRecovered(partition, lastHandledEventSequenceNumber, EventSequenceNumber.Unavailable);

    /// <inheritdoc/>
    /// <remarks>
    /// Live delivery skips a failed partition, so an event appended after the recovery read the partition but before
    /// the failure is cleared reaches the observer only through catch-up. The check for it therefore runs here, after
    /// the failure is cleared, from the furthest event the recovery read - including events the observer's filters
    /// exclude, which are never counted as handled.
    /// </remarks>
    public async Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber, FailedPartitionId? episodeId = null, Guid? deferralId = null)
    {
        if (IsRetired || _removed) return;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (!await ResolveFailedPartition(partition, lastHandledEventSequenceNumber, episodeId, deferralId)) return;
        Skips.Completing(partition, FurthestOf(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber));
        logger.FailingPartitionRecovered(partition);
        await StartCatchupJobIfNeeded(partition, FurthestOf(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber));
        EvaluateSuspectedSkips();
    }

    /// <summary>
    /// Adapts an internal failed delivery to its recording request.
    /// </summary>
    /// <param name="partition">The failed partition.</param>
    /// <param name="sequenceNumber">The failed event.</param>
    /// <param name="exceptionMessages">The failure messages.</param>
    /// <param name="exceptionStackTrace">The failure stack trace.</param>
    /// <param name="kind">The failure kind.</param>
    /// <param name="episodeId">The recovery episode.</param>
    /// <param name="deferralId">The recovery deferral.</param>
    /// <param name="context">The delivery context.</param>
    /// <returns>The failure recording task.</returns>
    Task RecordPartitionFailure(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        FailureKind kind,
        FailedPartitionId? episodeId,
        Guid? deferralId,
        PartitionFailureContext context) =>
        RecordPartitionFailure(new(partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind, episodeId, deferralId, context));
}
