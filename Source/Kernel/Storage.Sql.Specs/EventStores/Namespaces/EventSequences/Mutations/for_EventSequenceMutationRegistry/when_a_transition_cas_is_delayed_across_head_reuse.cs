// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_a_transition_cas_is_delayed_across_head_reuse : given.a_controlled_registry_interleaving
{
    EventSequenceMutationRegistryTransitionResult _delayed;
    EventSequenceMutationBeginResult _next;
    EventSequenceMutationBeginResult _observed;

    async Task Because()
    {
        var begin = await _registry.Begin(_request, _proposedTarget);
        var nextRequest = Request(_target, 43);
        var delayed = BeforeNextWrite(async () =>
        {
            var terminal = await Commit(_registry, begin);
            await _registry.Archive(_target, terminal.Token!);
            _next = await _registry.Begin(nextRequest, _proposedTarget);
        });
        _delayed = await delayed.Transition(_target, begin.Token!, EventSequenceMutationTransition.BeginApplying);
        _observed = await _registry.Begin(nextRequest, _proposedTarget);
    }

    [Fact] void should_reject_the_stale_cas_even_with_the_same_state_version() => _delayed.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.StateConflict);
    [Fact] void should_preserve_the_new_mutation() => _observed.Active.ShouldEqual(_next.Active);
}
