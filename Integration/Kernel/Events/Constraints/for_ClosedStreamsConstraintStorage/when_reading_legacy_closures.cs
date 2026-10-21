// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

using context = Cratis.Chronicle.Kernel.Integration.Events.Constraints.for_ClosedStreamsConstraintStorage.when_reading_legacy_closures.context;

namespace Cratis.Chronicle.Kernel.Integration.Events.Constraints.for_ClosedStreamsConstraintStorage;

[Collection(ChronicleCollection.Name)]
public class when_reading_legacy_closures(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public ClosedStream Read;
        public ClosedStreamDimensions[] Masks;
        public bool Reopened;

        readonly EventSequenceId _sequence = $"legacy-closures-{Guid.NewGuid():N}";
        IMongoCollection<BsonDocument> _collection;
        IClosedStreamsConstraintStorage _closures;

        async Task Establish()
        {
            var store = (Concepts.EventStoreName)Constants.EventStore;
            _collection = Services.GetRequiredService<IDatabase>().GetEventStoreDatabase(store)
                .GetNamespaceDatabase(Concepts.EventStoreNamespaceName.Default).GetCollection<BsonDocument>($"{_sequence}+closed_streams");
            await _collection.InsertOneAsync(new BsonDocument { { "streamType", "accounting" }, { "streamId", "period" } });
            _closures = Services.GetRequiredService<IStorage>().GetEventStore(store).GetNamespace(Concepts.EventStoreNamespaceName.Default).GetClosedStreamsConstraints(_sequence);
        }

        async Task Because()
        {
            Masks = [.. await _closures.GetDimensionsInUse()];
            Read = (await _closures.GetCovering(new(EventSourceId: "source", EventStreamType: "accounting", EventStreamId: "period"), Masks)).Single();
            Reopened = await _closures.Reopen(ClosedStreamOwner.Manual, Read.Scope);
        }

        async Task Destroy() => await _collection.Database.DropCollectionAsync(_collection.CollectionNamespace.CollectionName);
    }

    [Fact] void should_treat_the_legacy_row_as_manual() => Context.Read.Owner.ShouldEqual(ClosedStreamOwner.Manual);
    [Fact] void should_use_the_legacy_stream_dimensions() => Context.Masks.ShouldContainOnly(ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId);
    [Fact] void should_mark_the_unknown_sequence_number_unavailable() => Context.Read.SequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_allow_exact_manual_repair_of_a_legacy_row() => Context.Reopened.ShouldBeTrue();
}
