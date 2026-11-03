// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Calls the batch methods of a reactor for a page of events.
/// </summary>
internal static class ReactorBatches
{
    /// <summary>
    /// Calls every batch method with the events of the types it observes and collects the problems it reports.
    /// </summary>
    /// <param name="invoker">The <see cref="ReactorInvoker"/> of the activated reactor.</param>
    /// <param name="events">The events for batch methods, with their partition, in sequence order.</param>
    /// <param name="context">The <see cref="BatchContext"/> of the page.</param>
    /// <param name="canDefer">Whether the kernel accepts deferred partitions.</param>
    /// <returns>The <see cref="PartitionProblem"/> instances reported.</returns>
    public static async Task<IReadOnlyList<PartitionProblem>> Run(
        ReactorInvoker invoker,
        IReadOnlyList<(string Partition, EventAndContext Event)> events,
        BatchContext context,
        bool canDefer)
    {
        var problems = new List<PartitionProblem>();
        foreach (var method in invoker.Batches)
        {
            var observed = events.Where(_ => method.Observes(_.Event.Event.GetType())).ToArray();
            if (observed.Length == 0)
            {
                continue;
            }

            var batch = new EventBatch([.. observed.Select(_ => _.Event)], canDefer);
            try
            {
                await invoker.InvokeBatch(method, batch, context);
                problems.AddRange(batch.Outcomes.Select(outcome => ToProblem(observed, outcome)));
            }
            catch (Exception ex)
            {
                problems.AddRange(observed
                    .GroupBy(_ => _.Partition)
                    .Select(partition => PartitionProblem.Failed(partition.Key, partition.First().Event.Context.SequenceNumber, ex)));
            }
        }

        return problems;
    }

    static PartitionProblem ToProblem(IEnumerable<(string Partition, EventAndContext Event)> observed, BatchOutcome outcome)
    {
        var partition = observed.First(_ => _.Event.Context.EventSourceId == outcome.Partition).Partition;
        return outcome.Kind == BatchOutcomeKind.Deferred
            ? new(partition, outcome.At, ObservationState.Deferred, [], string.Empty, outcome.RetryAfter)
            : PartitionProblem.Failed(partition, outcome.At, outcome.Exception ?? new PartitionFailedInBatch(outcome.Partition));
    }
}
