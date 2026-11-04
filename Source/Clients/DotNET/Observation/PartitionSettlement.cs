// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Settles the outcome of a partition from the problems the methods of an observer reported for it.
/// </summary>
internal static class PartitionSettlement
{
    /// <summary>
    /// Settles a partition: it succeeds when no method reported a problem, and otherwise settles at the earliest problem.
    /// </summary>
    /// <param name="partition">The partition.</param>
    /// <param name="sequenceNumbers">The sequence numbers of the partition's events, in order.</param>
    /// <param name="problems">The problems reported by any method, for any partition.</param>
    /// <returns>The <see cref="PartitionOutcome"/> of the partition.</returns>
    public static PartitionOutcome Settle(string partition, IReadOnlyList<EventSequenceNumber> sequenceNumbers, IEnumerable<PartitionProblem> problems)
    {
        var earliest = problems.Where(_ => _.Partition == partition).OrderBy(_ => _.At.Value).FirstOrDefault();
        if (earliest is null)
        {
            return new(partition, ObservationState.Success, sequenceNumbers.Count > 0 ? sequenceNumbers[^1] : EventSequenceNumber.Unavailable, [], string.Empty);
        }

        var handled = sequenceNumbers.Where(_ => _.Value < earliest.At.Value).ToArray();
        return new(partition, earliest.State, handled.Length > 0 ? handled[^1] : EventSequenceNumber.Unavailable, earliest.ExceptionMessages, earliest.ExceptionStackTrace)
        {
            RetryAfter = earliest.RetryAfter
        };
    }
}
