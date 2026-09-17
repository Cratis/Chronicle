// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_beginning_a_mutation;

/// <summary>
/// The first ever <see cref="IEventSequenceMutationRegistry.Begin"/> call for a target sequence with no head
/// document yet must upsert one and reserve the mutation, matching the in-memory registry's first-time outcome.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_it_is_the_first_time(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationBeginResult _result = default!;

    async Task Because() => _result = await Registry.Begin(Request, ProposedTarget);

    [Fact] void should_reserve_the_mutation() => _result.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
    [Fact] void should_carry_the_active_mutation() => _result.Active.ShouldNotBeNull();
    [Fact] void should_start_at_the_reserved_phase() => _result.Active!.Phase.ShouldEqual(EventSequenceMutationPhase.Reserved);
    [Fact] void should_start_at_the_first_state_version() => _result.Active!.StateVersion.ShouldEqual(EventSequenceMutationStateVersion.First);
    [Fact] void should_assign_the_first_ordinal() => _result.Active!.Ordinal.ShouldEqual(EventSequenceMutationOrdinal.First);
    [Fact] void should_carry_a_complete_token() => _result.Token.ShouldNotBeNull();
}
