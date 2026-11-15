// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_a_begin_cas_is_delayed_across_archive : given.a_controlled_registry_interleaving
{
    EventSequenceMutationBeginResult _delayed;
    EventSequenceMutationBeginResult _retry;

    async Task Because()
    {
        await _registry.BeginTracking(_target, EventSequenceMutationCoverage.Untracked);
        var delayed = BeforeNextWrite(async () =>
        {
            var intervening = await _registry.Begin(Request(_target, 43), _proposedTarget);
            var terminal = await Commit(_registry, intervening);
            await _registry.Archive(_target, terminal.Token!);
        });
        _delayed = await delayed.Begin(_request, _proposedTarget);
        _retry = await _registry.Begin(_request, _proposedTarget);
    }

    [Fact] void should_report_a_lost_ordinal_claim() => _delayed.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Contended);
    [Fact] void should_not_reuse_the_intervening_ordinal() => _retry.Active!.Ordinal.Value.ShouldEqual(2L);
}
