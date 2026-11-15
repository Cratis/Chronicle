// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_the_namespace_is_whitespace : given.a_reserved_name_check
{
    bool _result;

    void Establish() => Reserve("observers");

    void Because() => _result = ReservedEventSequenceRules.MayBeWritten(_reservedEventSequenceIds, EventStore, " ", "observers");

    [Fact] void should_leave_the_refusal_to_the_namespace_rule() => _result.ShouldBeTrue();
    [Fact] void should_not_call_storage() => _storage.ReceivedCalls().ShouldBeEmpty();
}
