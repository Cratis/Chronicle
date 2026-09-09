// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_beginning_a_mutation;

/// <summary>
/// Calling <see cref="IEventSequenceMutationRegistry.Begin"/> a second time with the exact same request must
/// resume the already-reserved mutation rather than reserving a new one - the head document already carries an
/// active mutation whose id matches the incoming request.
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_resuming_an_identical_request(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationBeginResult _first = default!;
    EventSequenceMutationBeginResult _resumed = default!;

    async Task Because()
    {
        _first = await Registry.Begin(Request, ProposedTarget);
        _resumed = await Registry.Begin(Request, ProposedTarget);
    }

    [Fact] void should_resume_the_mutation() => _resumed.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Resumed);
    [Fact] void should_return_the_same_mutation_identifier() => _resumed.Active!.Id.ShouldEqual(_first.Active!.Id);
    [Fact] void should_return_the_same_ordinal() => _resumed.Active!.Ordinal.ShouldEqual(_first.Active!.Ordinal);
    [Fact] void should_return_the_same_state_version() => _resumed.Active!.StateVersion.ShouldEqual(_first.Active!.StateVersion);
    [Fact] void should_not_allocate_a_new_ordinal() => _resumed.Active!.Ordinal.ShouldEqual(EventSequenceMutationOrdinal.First);
}
