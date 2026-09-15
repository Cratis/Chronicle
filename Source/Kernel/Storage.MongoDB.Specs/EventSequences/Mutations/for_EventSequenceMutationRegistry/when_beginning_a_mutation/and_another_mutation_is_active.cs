// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_beginning_a_mutation;

/// <summary>
/// A different mutation request targeting the same event sequence while one is already active must be rejected -
/// only one mutation may be active per target sequence at a time.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_another_mutation_is_active(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationBeginResult _first = default!;
    EventSequenceMutationBeginResult _second = default!;

    async Task Because()
    {
        _first = await Registry.Begin(Request, ProposedTarget);
        var otherRequest = BuildRequest(Target, originSequenceNumber: 99);
        _second = await Registry.Begin(otherRequest, ProposedTarget);
    }

    [Fact] void should_report_a_mutation_already_in_progress() => _second.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.MutationAlreadyInProgress);
    [Fact] void should_carry_the_active_mutation_identifier() => _second.ConflictingMutationId.ShouldEqual(_first.Active!.Id);
}
