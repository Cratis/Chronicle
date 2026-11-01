// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceNamesFilter.when_executing;

public class and_the_command_does_not_write_to_an_event_sequence : given.a_reserved_event_sequence_names_filter
{
    CommandResult _result;

    async Task Because() => _result = await _filter.OnExecution(ContextFor(new object()));

    [Fact] void should_return_no_result() => _result.ShouldBeNull();
    [Fact] void should_not_ask_for_reserved_names() => _reservedEventSequenceIds.ReceivedCalls().ShouldBeEmpty();
}
