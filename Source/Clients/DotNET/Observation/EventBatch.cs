// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the events a batch method is called with, in sequence order, and lets it report partitions it could not handle.
/// </summary>
/// <remarks>
/// Returning normally means every partition in the batch succeeded. Throwing fails every partition in the batch at its first
/// event in the batch. <see cref="Fail(EventSourceId, Exception)"/> and <see cref="Defer(EventSourceId, TimeSpan)"/> report
/// a single partition, so the rest of the batch can still succeed.
/// </remarks>
public sealed class EventBatch : IReadOnlyList<EventAndContext>
{
    readonly IReadOnlyList<EventAndContext> _events;
    readonly bool _canDefer;
    readonly List<BatchOutcome> _outcomes = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBatch"/> class.
    /// </summary>
    /// <param name="events">The events, in sequence order.</param>
    /// <param name="canDefer">Whether the kernel accepts deferred partitions.</param>
    internal EventBatch(IReadOnlyList<EventAndContext> events, bool canDefer = true)
    {
        _events = events;
        _canDefer = canDefer;
    }

    /// <summary>
    /// Gets the partitions the batch holds events for, in the order their first event appears.
    /// </summary>
    public IEnumerable<EventSourceId> Partitions => _events.Select(_ => _.Context.EventSourceId).Distinct();

    /// <inheritdoc/>
    public int Count => _events.Count;

    /// <summary>
    /// Gets the outcomes reported for single partitions.
    /// </summary>
    internal IReadOnlyList<BatchOutcome> Outcomes => _outcomes;

    /// <inheritdoc/>
    public EventAndContext this[int index] => _events[index];

    /// <summary>
    /// Fails a partition from its first event in the batch.
    /// </summary>
    /// <param name="partition">The <see cref="EventSourceId"/> of the partition.</param>
    /// <param name="exception">The <see cref="Exception"/> describing the failure.</param>
    public void Fail(EventSourceId partition, Exception exception) =>
        Report(partition, FirstSequenceNumberOf(partition), BatchOutcomeKind.Failed, exception, TimeSpan.Zero);

    /// <summary>
    /// Fails the partition of an event, from that event.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> of the event the partition fails at.</param>
    /// <param name="exception">The <see cref="Exception"/> describing the failure.</param>
    public void Fail(EventContext context, Exception exception) =>
        Report(context.EventSourceId, context.SequenceNumber, BatchOutcomeKind.Failed, exception, TimeSpan.Zero);

    /// <summary>
    /// Defers a partition: it is not failed, and is delivered again from its first event in the batch after a delay.
    /// </summary>
    /// <param name="partition">The <see cref="EventSourceId"/> of the partition.</param>
    /// <param name="retryAfter">How long to wait before the partition is delivered again.</param>
    public void Defer(EventSourceId partition, TimeSpan retryAfter) =>
        Report(partition, FirstSequenceNumberOf(partition), BatchOutcomeKind.Deferred, null, retryAfter);

    /// <summary>
    /// Defers the partition of an event: it is not failed, and is delivered again from that event after a delay.
    /// </summary>
    /// <param name="context">The <see cref="EventContext"/> of the event to deliver again from.</param>
    /// <param name="retryAfter">How long to wait before the partition is delivered again.</param>
    public void Defer(EventContext context, TimeSpan retryAfter) =>
        Report(context.EventSourceId, context.SequenceNumber, BatchOutcomeKind.Deferred, null, retryAfter);

    /// <inheritdoc/>
    public IEnumerator<EventAndContext> GetEnumerator() => _events.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    EventSequenceNumber FirstSequenceNumberOf(EventSourceId partition) =>
        _events.FirstOrDefault(_ => _.Context.EventSourceId == partition)?.Context.SequenceNumber ?? throw new PartitionNotInBatch(partition);

    void Report(EventSourceId partition, EventSequenceNumber at, BatchOutcomeKind kind, Exception? exception, TimeSpan retryAfter)
    {
        if (!_events.Any(_ => _.Context.EventSourceId == partition))
        {
            throw new PartitionNotInBatch(partition);
        }

        if (kind == BatchOutcomeKind.Deferred && !_canDefer)
        {
            kind = BatchOutcomeKind.Failed;
            exception = new DeferralNotSupportedByKernel(partition);
        }

        _outcomes.Add(new(partition, at, kind, exception, retryAfter));
    }
}
