// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry;

[Collection(MongoDBCollection.Name)]
public class when_the_ordinal_is_exhausted(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationBeginResult _result;
    long _ordinal;

    Task Establish() => Database.GetCollection<EventSequenceMutationHeadEntry>(WellKnownCollectionNames.EventSequenceMutationHeads)
        .InsertOneAsync(new(Target.Display, EventSequenceMutationCoverage.Untracked, long.MaxValue, null));

    async Task Because()
    {
        _result = await Registry.Begin(Request, ProposedTarget);
        var head = await Database.GetCollection<EventSequenceMutationHeadEntry>(WellKnownCollectionNames.EventSequenceMutationHeads)
            .Find(Builders<EventSequenceMutationHeadEntry>.Filter.Empty).SingleAsync();
        _ordinal = head.LastAssignedOrdinal.Value;
    }

    [Fact] void should_fail_closed() => _result.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Corrupt);
    [Fact] void should_not_wrap_or_change_the_counter() => _ordinal.ShouldEqual(long.MaxValue);
}
