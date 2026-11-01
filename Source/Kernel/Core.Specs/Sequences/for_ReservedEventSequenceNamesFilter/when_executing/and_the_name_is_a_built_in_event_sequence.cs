// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceNamesFilter.when_executing;

public class and_the_name_is_a_built_in_event_sequence : given.a_reserved_event_sequence_names_filter
{
    CommandResult _result;

    void Establish() => Reserve("event-log");

    async Task Because() => _result = await _filter.OnExecution(ContextFor(AppendTo("event-log")));

    [Fact] void should_return_no_result() => _result.ShouldBeNull();
    [Fact] void should_not_touch_storage() => _storage.ReceivedCalls().ShouldBeEmpty();
}
