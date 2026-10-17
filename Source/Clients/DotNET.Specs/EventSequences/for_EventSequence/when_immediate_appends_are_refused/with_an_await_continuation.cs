// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_an_await_continuation : given.a_unit_with_native_sequence
{
    async Task Because()
    {
        using var frame = Refuse();
        ClearCalls();
        await Task.Yield();
        _error = await Catch.Exception(() => _sequence.Append("source", "changed"));
    }

    [Fact] void should_flow_across_await() => _error.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_not_reach_transport() => _sequences.ReceivedCalls().ShouldBeEmpty();
}
