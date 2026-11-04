// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_using_a_reserved_name;

public class and_the_sequence_is_new_when_using_an_idempotency_key : given.an_event_sequence_with_a_reserved_name
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.AppendWithIdempotencyKey(null!, null!, CorrelationId.New(), [], Identity.NotSet, ConcurrencyScope.None));

    [Fact] void should_refuse_the_name() => _error.ShouldBeOfExactType<ReservedEventSequenceName>();
    [Fact] void should_not_open_sequence_storage() => _namespaceStorage.DidNotReceive().GetEventSequence(SequenceId);
    [Fact] void should_not_write_state() => StateWrites.ShouldEqual(0);
}
