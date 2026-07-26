// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety;

/// <summary>
/// Folds a running total and records every sequence number it is handed.
/// </summary>
/// <remarks>
/// The recording is the anti-vacuity evidence the crash scenario needs. A green run that never saw a sequence
/// number twice proves only that the checkpoint happened to be durable at the moment of the crash, which is
/// indistinguishable from the fault not having been reproduced at all.
/// </remarks>
[DependencyInjection.IgnoreConvention]
public class RunningTotalReducer : IReducerFor<RunningTotal>
{
    readonly ConcurrentBag<ulong> _observedSequenceNumbers = [];

    /// <summary>
    /// Gets the number of sequence numbers that were handed to the reducer more than once.
    /// </summary>
    public int RedeliveredSequenceNumberCount =>
        _observedSequenceNumbers.GroupBy(_ => _).Count(group => group.Count() > 1);

    /// <summary>
    /// Folds an <see cref="AmountRecorded"/> into the running total.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The current read model.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <returns>The updated read model.</returns>
    public Task<RunningTotal?> OnAmountRecorded(AmountRecorded @event, RunningTotal? current, EventContext context)
    {
        _observedSequenceNumbers.Add(context.SequenceNumber);
        return Task.FromResult<RunningTotal?>(new RunningTotal(
            context.EventSourceId.ToString(),
            (current?.Total ?? 0) + @event.Amount));
    }
}
