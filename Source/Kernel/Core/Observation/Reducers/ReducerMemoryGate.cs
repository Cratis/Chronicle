// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Memory;
using Cratis.DependencyInjection;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Makes reduces of large states wait for room in the <see cref="IMemoryBudget"/>.
/// </summary>
/// <remarks>
/// A reduce holds several copies of the state at once, so the cost of one grows with the state, and nothing else
/// limits how many run at the same time. The size of a state is not known before it has been read, so the gate
/// remembers how large the state of a partition was the last time it was reduced and reserves for that; the first
/// reduce of a partition after a restart is only accounted for once its state has been read.
/// </remarks>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="budget">The <see cref="IMemoryBudget"/> to reserve from. When not available nothing is gated.</param>
/// <param name="meter">Optional <see cref="IMeter{T}"/> to report reducer state sizes to.</param>
[Singleton]
public class ReducerMemoryGate(
    IOptions<ChronicleOptions> options,
    IMemoryBudget? budget = null,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<ReducerMemoryGate>? meter = null)
{
    const int MaximumRememberedPartitions = 10_000;

    readonly MemoryManagement _settings = options.Value.Memory;
    readonly Histogram<long>? _stateSize = meter?.ActualMeter.CreateHistogram<long>(
        "cratis.chronicle.reducer.state_size",
        "By",
        "Estimated size of the state of a reducer read model when a reduce starts");
    readonly ConcurrentDictionary<(string Scope, Key Partition), long> _sizes = new();

    /// <summary>
    /// Wait for room to reduce a partition.
    /// </summary>
    /// <param name="scope">What the partition belongs to, so that equal partition keys of different read models are told apart.</param>
    /// <param name="partition">The <see cref="Key"/> of the partition.</param>
    /// <returns>The <see cref="ReducerMemoryTicket"/>, to dispose when the reduce is done.</returns>
    public async ValueTask<ReducerMemoryTicket> Enter(string scope, Key partition)
    {
        if (budget is null || !_settings.Enabled || !_sizes.TryGetValue((scope, partition), out var size))
        {
            return ReducerMemoryTicket.None;
        }

        var cost = CostOf(size);
        return cost < _settings.MinimumReservationBytes ? ReducerMemoryTicket.None : new ReducerMemoryTicket(await budget.Reserve(cost));
    }

    /// <summary>
    /// Account for a state that has been read, and remember its size for the next reduce of the partition.
    /// </summary>
    /// <param name="ticket">The <see cref="ReducerMemoryTicket"/> of the reduce.</param>
    /// <param name="scope">What the partition belongs to.</param>
    /// <param name="partition">The <see cref="Key"/> of the partition.</param>
    /// <param name="state">The state, when there is one.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A reduce that had no room reserved because the size of its state was not known waits for room here, with the
    /// state it has read as the only thing it holds.
    /// </remarks>
    public async ValueTask Observe(ReducerMemoryTicket ticket, string scope, Key partition, ExpandoObject? state)
    {
        if (state is null)
        {
            return;
        }

        var size = ExpandoObjectSize.Estimate(state);
        _stateSize?.Record(size);

        if (budget is null || !_settings.Enabled)
        {
            return;
        }

        if (_sizes.Count >= MaximumRememberedPartitions)
        {
            _sizes.Clear();
        }

        _sizes[(scope, partition)] = size;

        var cost = CostOf(size);
        if (cost < _settings.MinimumReservationBytes)
        {
            return;
        }

        if (ticket.Reservation.Bytes == 0)
        {
            ticket.Reservation = await budget.Reserve(cost);
        }
        else if (cost > ticket.Reservation.Bytes)
        {
            ticket.Reservation.Grow(cost - ticket.Reservation.Bytes);
        }
    }

    long CostOf(long size) => size * Math.Max(1, _settings.ReducerCostMultiplier);
}
