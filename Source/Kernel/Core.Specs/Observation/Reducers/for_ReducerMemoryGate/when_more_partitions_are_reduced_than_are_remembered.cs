// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

/// <summary>
/// Reaching the number of remembered partitions forgets the oldest ones, not every size at once, so the bounds of a
/// reducer page keep counting the states of the partitions reduced just before.
/// </summary>
public class when_more_partitions_are_reduced_than_are_remembered : given.a_gate_with_a_budget
{
    const int Partitions = ReducerMemoryGate.MaximumRememberedPartitions + 1;

    long _expectedSize;
    long _recentlyReduced;
    long _latest;
    long _earliest;

    protected override MemoryManagement Settings => new() { Enabled = false };

    async Task Because()
    {
        var state = StateOfAboutBytes(100);
        _expectedSize = ExpandoObjectSize.Estimate(state);
        for (var index = 0; index < Partitions; index++)
        {
            await _gate.Observe(ReducerMemoryTicket.None, Scope, PartitionNumber(index), state);
        }

        _recentlyReduced = _gate.RememberedSizeOf(Scope, PartitionNumber(Partitions - 2));
        _latest = _gate.RememberedSizeOf(Scope, PartitionNumber(Partitions - 1));
        _earliest = _gate.RememberedSizeOf(Scope, PartitionNumber(0));
    }

    [Fact] void should_remember_the_partition_reduced_just_before() => _recentlyReduced.ShouldEqual(_expectedSize);
    [Fact] void should_remember_the_latest_partition() => _latest.ShouldEqual(_expectedSize);
    [Fact] void should_forget_the_earliest_partition() => _earliest.ShouldEqual(0L);

    static Key PartitionNumber(int index) => new(index.ToString(), ArrayIndexers.NoIndexers);
}
