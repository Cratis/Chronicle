// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_archiving_a_mutation;

/// <summary>
/// A mutation that has not yet reached <see cref="EventSequenceMutationPhase.SourceCommitted"/> is not eligible
/// for archiving - the freshly reserved mutation from <see cref="IEventSequenceMutationRegistry.Begin"/> is still
/// at the <see cref="EventSequenceMutationPhase.Reserved"/> phase.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/> providing the MongoDB server.</param>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_mutation_is_not_terminal(ReplicaSetMongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationRegistryArchiveResult _result = default!;

    async Task Because()
    {
        var begin = await Registry.Begin(Request, ProposedTarget);
        _result = await Registry.Archive(Target, begin.Token!);
    }

    [Fact] void should_reject_the_archive() => _result.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);
    [Fact] void should_carry_no_history() => _result.History.ShouldBeNull();
}
