// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Observation.Reducers.for_RememberedStateSizes;

public class when_remembering_more_than_its_capacity : Specification
{
    const string Scope = "store/namespace/read-model";
    const int Capacity = 10;
    const int Partitions = 100;

    RememberedStateSizes _sizes;
    bool _latestRemembered;
    long _latestSize;
    bool _earliestRemembered;

    void Establish() => _sizes = new(Capacity);

    void Because()
    {
        for (var index = 0; index < Partitions; index++)
        {
            _sizes.Remember(Scope, PartitionNumber(index), index);
        }

        _latestRemembered = _sizes.TryGet(Scope, PartitionNumber(Partitions - 1), out _latestSize);
        _earliestRemembered = _sizes.TryGet(Scope, PartitionNumber(0), out _);
    }

    [Fact] void should_remember_the_latest_partition() => _latestRemembered.ShouldBeTrue();
    [Fact] void should_remember_the_size_of_the_latest_partition() => _latestSize.ShouldEqual(Partitions - 1);
    [Fact] void should_forget_the_earliest_partition() => _earliestRemembered.ShouldBeFalse();
    [Fact] void should_hold_no_more_than_its_capacity() => _sizes.Count.ShouldBeLessThanOrEqual(Capacity);

    static Key PartitionNumber(int index) => new(index.ToString(), ArrayIndexers.NoIndexers);
}
