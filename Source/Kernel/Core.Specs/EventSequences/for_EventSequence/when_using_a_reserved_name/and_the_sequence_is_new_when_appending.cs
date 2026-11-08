// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_using_a_reserved_name;

public class and_the_sequence_is_new_when_appending : given.an_event_sequence_with_a_reserved_name
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => AppendAnEvent());

    [Fact] void should_refuse_the_name() => _error.ShouldBeOfExactType<ReservedEventSequenceName>();
    [Fact] void should_explain_the_refusal() => _error.Message.ShouldEqual(ReservedEventSequenceRules.Message(SequenceId.Value));
    [Fact] void should_name_the_member() => ((IValidationFailure)_error).ValidationResult.Members.ShouldContain("eventSequenceId");
    [Fact] void should_not_open_sequence_storage() => _namespaceStorage.DidNotReceive().GetEventSequence(Arg.Any<EventSequenceId>());
    [Fact] void should_not_append() => _appendedSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_write_state() => StateWrites.ShouldEqual(0);
    [Fact] void should_not_create_indexes() => _eventSequenceStorage.DidNotReceive().EnsureIndexes();
    [Fact] void should_not_ensure_the_namespace() => _namespaces.DidNotReceive().Ensure(Arg.Any<EventStoreNamespaceName>());
}
