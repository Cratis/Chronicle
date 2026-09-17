// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_recovering_an_archive_before_head_release : given.a_controlled_registry_interleaving
{
    Exception _failure;
    EventSequenceMutationRegistryArchiveResult _retry;
    EventSequenceMutationRegistryArchiveResult _afterReuse;
    EventSequenceMutationBeginResult _next;
    EventSequenceMutationBeginResult _observed;

    async Task Because()
    {
        var begin = await _registry.Begin(_request, _proposedTarget);
        var terminal = await Commit(_registry, begin);
        var crashing = BeforeNextWrite(() => throw new simulated_crash());
        _failure = await Catch.Exception(() => crashing.Archive(_target, terminal.Token!));
        var recreated = new EventSequenceMutationRegistry(_eventStore, _namespace, _database);
        _retry = await recreated.Archive(_target, terminal.Token!);
        var nextRequest = Request(_target, 43);
        _next = await recreated.Begin(nextRequest, _proposedTarget);
        _afterReuse = await recreated.Archive(_target, terminal.Token!);
        _observed = await recreated.Begin(nextRequest, _proposedTarget);
    }

    [Fact] void should_have_interrupted_the_release() => _failure.ShouldBeOfExactType<simulated_crash>();
    [Fact] void should_recover_the_durable_receipt() => _retry.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived);
    [Fact] void should_release_the_exact_terminal_head() => _next.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
    [Fact] void should_preserve_idempotency_after_reuse() => _afterReuse.History.ShouldEqual(_retry.History);
    [Fact] void should_not_clear_the_new_head() => _observed.Active.ShouldEqual(_next.Active);

    sealed class simulated_crash : Exception;
}
