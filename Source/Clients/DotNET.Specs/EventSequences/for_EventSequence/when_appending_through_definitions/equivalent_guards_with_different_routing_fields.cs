// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_through_definitions;

public class equivalent_guards_with_different_routing_fields : given.a_definition_backed_batch
{
    async Task Because() => await Append([Payment(), CartWithoutStream("other"), CartWithoutStream("another")]);

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_send_one_scope() => _request.ConcurrencyScopes.Count().ShouldEqual(1);
    [Fact] void should_guard_on_the_source_id_only() => _request.ConcurrencyScopes.Single().Scope.EventStreamId.ShouldBeNull();
    [Fact] void should_keep_the_earliest_sequence_number() => _request.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(10UL);
}
