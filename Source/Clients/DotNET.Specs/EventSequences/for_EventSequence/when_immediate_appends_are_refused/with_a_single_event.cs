// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_a_single_event : given.a_unit_with_native_sequence
{
    void Because()
    {
        using var frame = Refuse();
        ClearCalls();
        _error = Catch.Exception(new Action(() => _ = _sequence.Append("source", "changed")));
    }

    [Fact] void should_throw_synchronously() => _error.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_not_serialize() => _eventSerializer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_reach_transport() => _sequences.ReceivedCalls().ShouldBeEmpty();
}
