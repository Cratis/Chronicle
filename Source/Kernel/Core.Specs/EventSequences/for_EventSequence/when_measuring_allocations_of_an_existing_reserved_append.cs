// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_measuring_allocations_of_an_existing_reserved_append : given.an_event_sequence_with_a_reserved_name
{
    AppendResult _result;
    protected override bool SequenceExists => true;

    async Task Because() => await AllocationProbe.Measure("existing-reserved-append", 300, async () => _result = await AppendAnEvent());

    [Fact] void should_append() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_recheck_existence() => _storage.Received(1).GetEventStores();
}
