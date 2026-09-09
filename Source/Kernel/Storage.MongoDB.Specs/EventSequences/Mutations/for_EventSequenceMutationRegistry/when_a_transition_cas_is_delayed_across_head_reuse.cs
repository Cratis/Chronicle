// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry;

[Collection(MongoDBCollection.Name)]
public class when_a_transition_cas_is_delayed_across_head_reuse(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationRegistryTransitionResult _delayed;
    EventSequenceMutationBeginResult _next;
    EventSequenceMutationBeginResult _observed;

    async Task Because()
    {
        var begin = await Registry.Begin(Request, ProposedTarget);
        var nextRequest = BuildRequest(Target, 43);
        var delayed = BeforeNextClaimOrTransition(async () =>
        {
            var terminal = await Commit(begin);
            await Registry.Archive(Target, terminal.Token!);
            _next = await Registry.Begin(nextRequest, ProposedTarget);
        });
        _delayed = await delayed.Transition(Target, begin.Token!, EventSequenceMutationTransition.BeginApplying);
        _observed = await Registry.Begin(nextRequest, ProposedTarget);
    }

    [Fact] void should_reject_the_stale_cas_even_with_the_same_state_version() => _delayed.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.StateConflict);
    [Fact] void should_preserve_the_new_mutation() => _observed.Active.ShouldEqual(_next.Active);
}
