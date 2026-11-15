// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_archiving_and_the_head_changes_before_release : given.a_controlled_registry_interleaving
{
    EventSequenceMutationRegistryArchiveResult _result;
    int _historyCount;
    EventSequenceMutationBeginResult _next;

    async Task Because()
    {
        var begin = await _registry.Begin(_request, _proposedTarget);
        var terminal = await Commit(_registry, begin);
        var delayed = BeforeNextTransaction(async () =>
        {
            await using var context = CreateContext();
            await context.EventSequenceMutationHeads
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.ActiveRepairState, EventSequenceMutationRepairState.Pending));
        });
        _result = await delayed.Archive(_target, terminal.Token!);
        await using var verification = CreateContext();
        _historyCount = await verification.EventSequenceMutationHistory.CountAsync();
        _next = await _registry.Begin(Request(_target, 43), _proposedTarget);
    }

    [Fact] void should_report_a_state_conflict() => _result.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);
    [Fact] void should_not_write_a_history_entry() => _historyCount.ShouldEqual(0);
    [Fact] void should_keep_the_head_active() => _next.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.MutationAlreadyInProgress);
}
