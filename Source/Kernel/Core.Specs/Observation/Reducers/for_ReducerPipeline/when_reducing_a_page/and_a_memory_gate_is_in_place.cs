// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Memory;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page;

/// <summary>
/// The states of a page are held at the same time, so the page waits for room for all of them with one reservation.
/// </summary>
public class and_a_memory_gate_is_in_place : given.a_page_pipeline
{
    CountingBudget _budget;
    long _reservedWhileWithTheClient;

    void Establish()
    {
        foreach (var partition in Partitions)
        {
            _sink.States[partition] = LargeState();
        }
    }

    async Task Because()
    {
        // The first page learns how large each state is; the second reserves for all of them up front.
        await ReduceThePage();
        _budget.Reservations = 0;
        _outcome = await _pipeline.ReducePage(Page(), partitions =>
        {
            _reservedWhileWithTheClient = _budget.Reserved;
            return Reduce(partitions);
        });
    }

    [Fact] void should_reserve_once_for_the_whole_page() => _budget.Reservations.ShouldEqual(1);
    [Fact] void should_hold_the_reservation_while_the_page_is_with_the_client() => _reservedWhileWithTheClient.ShouldBeGreaterThan(0L);
    [Fact] void should_give_it_back_when_the_page_is_done() => _budget.Reserved.ShouldEqual(0);

    protected override ReducerMemoryGate? CreateMemoryGate()
    {
        _budget = new CountingBudget(new MemoryBudget(() => 100_000_000));
        return new(Options.Create(new ChronicleOptions { Memory = new() { MinimumReservationBytes = 1000, ReducerCostMultiplier = 4 } }), _budget);
    }

    static ExpandoObject LargeState()
    {
        var state = new ExpandoObject();
        ((IDictionary<string, object?>)state)["text"] = new string('x', 50_000);
        return state;
    }

    sealed class CountingBudget(IMemoryBudget inner) : IMemoryBudget
    {
        public int Reservations { get; set; }

        public long Capacity => inner.Capacity;

        public long Reserved => inner.Reserved;

        public int Waiting => inner.Waiting;

        public ValueTask<MemoryReservation> Reserve(long bytes, CancellationToken cancellationToken = default)
        {
            Reservations++;
            return inner.Reserve(bytes, cancellationToken);
        }

        public bool TryReserve(long bytes, out MemoryReservation reservation)
        {
            Reservations++;
            return inner.TryReserve(bytes, out reservation);
        }
    }
}
