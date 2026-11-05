// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_beginning_a_mutation;

/// <summary>
/// A second <see cref="IEventSequenceMutationRegistry.Begin"/> call reusing the same mutation id but carrying a
/// different command payload cannot be a legitimate resume of the already-bound mutation.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_the_request_has_changed(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationBeginResult _first = default!;
    EventSequenceMutationBeginResult _conflicting = default!;

    async Task Because()
    {
        _first = await Registry.Begin(Request, ProposedTarget);
        var changedRequest = Request with { Command = new("{\"name\":\"changed\"}", "changed-hash") };
        _conflicting = await Registry.Begin(changedRequest, ProposedTarget);
    }

    [Fact] void should_report_a_definition_conflict() => _conflicting.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);
    [Fact] void should_carry_the_conflicting_mutation_identifier() => _conflicting.ConflictingMutationId.ShouldEqual(Request.Id);
    [Fact] void should_leave_the_original_reservation_untouched() => _first.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
}
