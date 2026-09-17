// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_archiving_a_mutation;

/// <summary>
/// A mutation that has reached <see cref="EventSequenceMutationPhase.SourceCommitted"/> is archived: the terminal
/// receipt is inserted into the history collection and the head's active mutation is cleared. A retry that races
/// the original archive - inserting the identical terminal receipt first - is recognized as the same outcome
/// rather than surfaced as a duplicate-key storage error, and recovery releases only the matching terminal head.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_mutation_is_terminal(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationRegistryArchiveResult _archived = default!;
    EventSequenceMutationRegistryArchiveResult _raceRetry = default!;
    EventSequenceMutationBeginResult _next = default!;
    EventSequenceMutationBeginResult _observed = default!;

    async Task Because()
    {
        var begin = await Registry.Begin(Request, ProposedTarget);
        var applying = await Apply(Registry, begin, EventSequenceMutationTransition.BeginApplying);
        var verifying = await Registry.Transition(Target, applying.Token!, EventSequenceMutationTransition.BeginVerifying);
        var committed = await Registry.Transition(Target, verifying.Token!, EventSequenceMutationTransition.CommitSourceWithoutRepair);

        // Simulate a caller racing this exact archive: it also read 'committed.Active' and independently prepared
        // and inserted the identical terminal receipt before this call's own insert lands.
        var targetId = (EventSequenceId)Target.Display;
        var prepared = EventSequenceMutationStateMachine.PrepareArchive(committed.Token!.Scope, committed.Active!, committed.Token!);
        var racingHistory = prepared.History!;
        var racingDocument = new EventSequenceMutationHistoryEntry(
            racingHistory.Id,
            targetId,
            racingHistory.Ordinal,
            racingHistory.Origin,
            racingHistory.Kind,
            racingHistory.CommandHash,
            racingHistory.Target,
            racingHistory.RepairState,
            racingHistory.TerminalWitness);
        await Database.GetCollection<EventSequenceMutationHistoryEntry>(WellKnownCollectionNames.EventSequenceMutationHistory)
            .InsertOneAsync(racingDocument);

        _archived = await Registry.Archive(Target, committed.Token!);

        var nextRequest = BuildRequest(Target, 43);
        _next = await Registry.Begin(nextRequest, ProposedTarget);
        _raceRetry = await RegistryOver(Database).Archive(Target, committed.Token!);
        _observed = await Registry.Begin(nextRequest, ProposedTarget);
    }

    [Fact] void should_recognize_the_racing_insert_as_already_archived() => _archived.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived);
    [Fact] void should_return_the_racing_receipt() => _archived.History!.Id.ShouldEqual(Request.Id);
    [Fact] void should_return_a_payload_free_receipt() => _archived.History!.CommandHash.ShouldEqual(Request.Command.Hash);
    [Fact] void should_be_the_same_outcome_on_every_read() => _raceRetry.Outcome.ShouldEqual(_archived.Outcome);
    [Fact] void should_release_the_crashed_archive_head() => _next.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
    [Fact] void should_preserve_the_new_head_on_retry() => _observed.Active.ShouldEqual(_next.Active);
}
