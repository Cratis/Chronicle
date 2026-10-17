// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_an_async_entry : given.a_unit_with_native_sequence
{
    bool _returnedTask;
    bool _faultedTask;

    async Task Because()
    {
        using var frame = Refuse();
        ClearCalls();
        _error = await Catch.Exception(async () =>
        {
            var task = _sequence.AppendThroughEventSource(typeof(object), "source", "changed");
            _returnedTask = true;
            _faultedTask = task.IsFaulted;
            await task;
        });
    }

    [Fact] void should_return_a_faulted_task() => _faultedTask.ShouldBeTrue();
    [Fact] void should_not_throw_before_returning_the_task() => _returnedTask.ShouldBeTrue();
    [Fact] void should_fault_with_the_append_refusal() => _error.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_not_reach_transport() => _sequences.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_resolve_providers() => _eventSources.ReceivedCalls().ShouldBeEmpty();
}
