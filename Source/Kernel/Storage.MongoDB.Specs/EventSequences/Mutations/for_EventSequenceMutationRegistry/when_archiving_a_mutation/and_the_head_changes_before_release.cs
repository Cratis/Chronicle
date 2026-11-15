// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_archiving_a_mutation;

/// <summary>
/// The exact terminal head moves after the archive has read it and before the transactional release. The archive
/// must not leave a receipt behind and must leave the head untouched.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/> providing the MongoDB server.</param>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_head_changes_before_release(ReplicaSetMongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationRegistryArchiveResult _result = default!;
    long _historyCount;
    EventSequenceMutationBeginResult _next = default!;

    async Task Because()
    {
        var begin = await Registry.Begin(Request, ProposedTarget);
        var committed = await Commit(begin);
        var heads = Database.GetCollection<EventSequenceMutationHeadEntry>(WellKnownCollectionNames.EventSequenceMutationHeads);
        var interleaving = BeforeNextArchiveRelease(async () => await heads.UpdateOneAsync(
            Builders<EventSequenceMutationHeadEntry>.Filter.Eq(_ => _.EventSequenceId, (Concepts.EventSequences.EventSequenceId)Target.Display),
            Builders<EventSequenceMutationHeadEntry>.Update.Set(_ => _.Active!.RepairState, EventSequenceMutationRepairState.Pending)));

        _result = await interleaving.Archive(Target, committed.Token!);
        _historyCount = await Database.GetCollection<EventSequenceMutationHistoryEntry>(WellKnownCollectionNames.EventSequenceMutationHistory)
            .CountDocumentsAsync(Builders<EventSequenceMutationHistoryEntry>.Filter.Empty);
        _next = await Registry.Begin(BuildRequest(Target, 43), ProposedTarget);
    }

    [Fact] void should_report_a_state_conflict() => _result.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);
    [Fact] void should_not_write_a_history_entry() => _historyCount.ShouldEqual(0L);
    [Fact] void should_keep_the_head_active() => _next.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.MutationAlreadyInProgress);
}
