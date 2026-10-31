// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_storage_throws_after_the_event_is_durable : given.an_append_with_settlement
{
    void Establish() => _throwAfterWrite = true;
    async Task Because() => _result = await AppendAnEvent();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_probe_the_slot() => _probes.ShouldEqual(1);
    [Fact] async Task should_advance_the_number() => (await _eventSequence.GetNextSequenceNumber()).ShouldEqual((Concepts.Events.EventSequenceNumber)1UL);
}
