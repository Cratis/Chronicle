// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_head_release_is_delayed_across_an_exact_retry : given.a_controlled_registry_interleaving
{
    EventSequenceMutationRegistryArchiveResult _original;
    EventSequenceMutationRegistryArchiveResult _retry;
    EventSequenceMutationRegistryTransitionResult _next;
    EventSequenceMutationBeginResult _observed;

    async Task Because()
    {
        var begin = await _registry.Begin(_request, _proposedTarget);
        var terminal = await Commit(_registry, begin);
        var nextRequest = Request(_target, 43);
        var delayed = BeforeNextWrite(async () =>
        {
            _retry = await _registry.Archive(_target, terminal.Token!);
            var next = await _registry.Begin(nextRequest, _proposedTarget);
            _next = await Commit(_registry, next);
        });
        _original = await delayed.Archive(_target, terminal.Token!);
        _observed = await _registry.Begin(nextRequest, _proposedTarget);
    }

    [Fact] void should_accept_the_original_archive() => _original.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.Archived);
    [Fact] void should_recover_the_concurrent_retry() => _retry.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived);
    [Fact] void should_not_release_a_later_terminal_head_with_the_same_state_version() => _observed.Active.ShouldEqual(_next.Active);
}
