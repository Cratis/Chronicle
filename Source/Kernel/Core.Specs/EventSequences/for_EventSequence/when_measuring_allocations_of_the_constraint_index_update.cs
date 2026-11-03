// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_measuring_allocations_of_the_constraint_index_update : given.an_event_sequence_with_a_failing_index
{
    long _bytes;

    async Task Because() => (_bytes, _) = await AllocationProbe.Measure("append-with-unique-constraint", 300, async () => _result = await AppendAnEvent());

    [Fact] void should_still_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_stay_below_a_generous_upper_bound() => _bytes.ShouldBeLessThan(4L * 1024 * 1024);
}
