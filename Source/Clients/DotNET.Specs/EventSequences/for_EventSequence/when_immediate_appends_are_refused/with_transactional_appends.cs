// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_transactional_appends : given.a_unit_with_native_sequence
{
    object[] _enrolled;

    async Task Because()
    {
        using var frame = Refuse();
        ClearCalls();
        await _sequence.Transactional.Append("source", "single");
        await _sequence.Transactional.AppendWithNamedTags("source", "named", [new("key", "value")]);
        await _sequence.Transactional.AppendMany("source", ["batch one", "batch two"]);
        await _sequence.Transactional.AppendManyWithNamedTags("source", ["named batch"], [new("key", "value")]);
        _enrolled = _unit.GetEvents().ToArray();
    }

    [Fact] void should_enroll_every_transactional_variant() => _enrolled.ShouldEqual<object[]>(["single", "named", "batch one", "batch two", "named batch"]);
    [Fact] void should_not_reach_transport_during_enrollment() => _sequences.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_serialize_during_enrollment() => _eventSerializer.ReceivedCalls().ShouldBeEmpty();
}
