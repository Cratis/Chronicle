// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_beginning_for_the_first_time : given.a_mutation_registry
{
    EventSequenceMutationBeginResult _result;

    async Task Because() => _result = await _registry.Begin(_request, _proposedTarget);

    [Fact] void should_reserve_the_mutation() => _result.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
    [Fact] void should_return_the_exact_request() => _result.Active!.Definition.Request.ShouldEqual(_request);
    [Fact] void should_freeze_the_proposed_target() => _result.Active!.Target.ShouldEqual(_proposedTarget);
    [Fact] void should_assign_the_first_ordinal() => _result.Active!.Ordinal.Value.ShouldEqual(1L);
    [Fact] void should_start_at_state_version_one() => _result.Active!.StateVersion.Value.ShouldEqual(1L);
    [Fact] void should_start_reserved() => _result.Active!.Phase.ShouldEqual(EventSequenceMutationPhase.Reserved);
    [Fact] void should_return_the_exact_scope() => _result.Token!.Scope.ShouldEqual(new EventSequenceKey(_target.Display, "event-store", "namespace"));
}
