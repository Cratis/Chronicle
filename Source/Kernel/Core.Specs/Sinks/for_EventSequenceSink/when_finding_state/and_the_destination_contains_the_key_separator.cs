// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sinks.for_EventSequenceSink.when_finding_state;

public class and_the_destination_contains_the_key_separator : given.a_sink
{
    static readonly EventSequenceId _withSeparator = new("observers#store#ns");

    System.Dynamic.ExpandoObject _state;
    System.Dynamic.ExpandoObject _primaryState;

    protected override EventSequenceId? Configured => _withSeparator;

    async Task Because()
    {
        _state = await _sink.FindOrDefault("customer-1");
        _primaryState = await _sink.FindInPrimaryOrDefault("customer-1");
    }

    [Fact] void should_find_no_state() => _state.ShouldBeNull();
    [Fact] void should_find_no_primary_state() => _primaryState.ShouldBeNull();
    [Fact] void should_not_open_any_destination_storage() => _destinations.OpenedSequences.ShouldBeEmpty();
}
