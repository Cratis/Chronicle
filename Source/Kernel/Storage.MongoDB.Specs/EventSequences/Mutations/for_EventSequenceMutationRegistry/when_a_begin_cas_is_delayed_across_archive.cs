// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry;

[Collection(MongoDBCollection.Name)]
public class when_a_begin_cas_is_delayed_across_archive(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationBeginResult _delayed;
    EventSequenceMutationBeginResult _retry;

    async Task Because()
    {
        await Registry.BeginTracking(Target, EventSequenceMutationCoverage.Untracked);
        var delayed = BeforeNextClaimOrTransition(async () =>
        {
            var intervening = await Registry.Begin(BuildRequest(Target, 43), ProposedTarget);
            var terminal = await Commit(intervening);
            await Registry.Archive(Target, terminal.Token!);
        });
        _delayed = await delayed.Begin(Request, ProposedTarget);
        _retry = await Registry.Begin(Request, ProposedTarget);
    }

    [Fact] void should_report_a_lost_ordinal_claim() => _delayed.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Contended);
    [Fact] void should_not_reuse_the_intervening_ordinal() => _retry.Active!.Ordinal.Value.ShouldEqual(2L);
}
