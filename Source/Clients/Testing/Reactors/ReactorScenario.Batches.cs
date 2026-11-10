// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

public partial class ReactorScenario<TReactor>
{
    readonly List<BatchPartitionOutcome> _partitionOutcomes = [];

    /// <summary>
    /// Gets the partitions the reactor's batch methods reported as failed or deferred, in the order they were reported.
    /// </summary>
    /// <remarks>
    /// A batch method reports a single partition with <c language="csharp">batch.Fail(...)</c> or <c language="csharp">batch.Defer(...)</c>, so the rest of
    /// the batch still succeeds. A batch method that throws fails every partition in its batch instead, and the scenario
    /// surfaces that exception from the call that delivered the events.
    /// </remarks>
    public IReadOnlyList<BatchPartitionOutcome> PartitionOutcomes => _partitionOutcomes;

    /// <summary>
    /// Asserts that a batch method failed a partition.
    /// </summary>
    /// <param name="partition">The <see cref="EventSourceId"/> of the partition.</param>
    /// <exception cref="BatchPartitionOutcomeAssertionException">Thrown when no batch method failed the partition.</exception>
    public void ShouldHaveFailed(EventSourceId partition) => AssertOutcome(partition, BatchPartitionOutcomeKind.Failed);

    /// <summary>
    /// Asserts that a batch method deferred a partition.
    /// </summary>
    /// <param name="partition">The <see cref="EventSourceId"/> of the partition.</param>
    /// <exception cref="BatchPartitionOutcomeAssertionException">Thrown when no batch method deferred the partition.</exception>
    public void ShouldHaveDeferred(EventSourceId partition) => AssertOutcome(partition, BatchPartitionOutcomeKind.Deferred);

    /// <summary>
    /// Delivers events of several event sources as one live page.
    /// </summary>
    /// <param name="events">The events, in the order they are appended.</param>
    /// <returns>A task completing after every method handled its events.</returns>
    internal Task InvokeWithPage(IEnumerable<EventForEventSourceId> events) =>
        Deliver(events.SelectMany(@event => _occurrences.Record(@event.EventSourceId, [@event.Event], @event.EventSourceType, @event.EventStreamType, @event.EventStreamId)).ToArray());

    async Task DeliverToBatchMethods(ReactorInvoker invoker, IReadOnlyList<RecordedOccurrence> occurrences, IEventStore eventStore)
    {
        var context = PageContextFor(invoker, occurrences, eventStore);
        foreach (var method in invoker.Batches)
        {
            var observed = occurrences.Where(_ => method.Observes(_.Event.GetType())).Select(_ => new EventAndContext(_.Event, _.Context)).ToArray();
            if (observed.Length == 0)
            {
                continue;
            }

            var batch = new EventBatch(observed);
            await invoker.InvokeBatch(method, batch, context);
            _partitionOutcomes.AddRange(batch.Outcomes.Select(outcome => new BatchPartitionOutcome(
                outcome.Partition,
                outcome.At,
                outcome.Kind == BatchOutcomeKind.Deferred ? BatchPartitionOutcomeKind.Deferred : BatchPartitionOutcomeKind.Failed,
                outcome.Exception,
                outcome.RetryAfter)));
        }
    }

    BatchContext PageContextFor(ReactorInvoker invoker, IReadOnlyList<RecordedOccurrence> occurrences, IEventStore eventStore) => new(
        new ObserverId(invoker.RegisteredId?.Value ?? typeof(TReactor).FullName ?? typeof(TReactor).Name),
        eventStore.Name,
        eventStore.Namespace,
        invoker.RegisteredEventSequenceId ?? EventSequenceId.Log,
        ObservationOrigin.Live,
        [.. occurrences.Select(_ => _.Context.EventSourceId).Distinct()],
        occurrences.Count,
        occurrences.Count > 0 ? occurrences[0].Context.SequenceNumber : EventSequenceNumber.Unavailable,
        occurrences.Count > 0 ? occurrences[^1].Context.SequenceNumber : EventSequenceNumber.Unavailable,
        null,
        CancellationToken.None);

    void AssertOutcome(EventSourceId partition, BatchPartitionOutcomeKind kind)
    {
        if (_partitionOutcomes.Exists(_ => _.Partition == partition && _.Kind == kind))
        {
            return;
        }

        var reported = _partitionOutcomes.Count > 0
            ? string.Join(", ", _partitionOutcomes.Select(_ => $"{_.Kind} '{_.Partition.Value}'"))
            : "nothing";
        throw new BatchPartitionOutcomeAssertionException(
            $"Expected a batch method of reactor '{typeof(TReactor).Name}' to report partition '{partition.Value}' as {kind}, but it reported: {reported}.");
    }
}
