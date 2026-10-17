// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_a_raw_read : given.a_unit_with_native_sequence
{
    EventSequenceNumber _tail;

    async Task Because()
    {
        using var frame = Refuse();
        ClearCalls();
        _error = await Catch.Exception(async () => _tail = await _sequence.GetTailSequenceNumber());
    }

    [Fact] void should_leave_read_admission_unchanged() => _error.ShouldBeNull();
    [Fact] void should_read_the_real_response() => _tail.ShouldEqual((EventSequenceNumber)40);
    [Fact] async Task should_reach_the_read_transport() => await _sequences.Received(1).TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), Arg.Any<CallContext>());
    [Fact] void should_not_turn_on_read_protection() => _unit.IsEnforcingReadProtection.ShouldBeFalse();
}
