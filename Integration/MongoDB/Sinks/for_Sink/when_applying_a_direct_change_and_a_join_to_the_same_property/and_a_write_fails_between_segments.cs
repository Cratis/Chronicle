// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_applying_a_direct_change_and_a_join_to_the_same_property;

[Collection(MongoDBCollection.Name)]
public class and_a_write_fails_between_segments(ChronicleInProcessFixture fixture) : given.a_sink_over_real_mongo(fixture)
{
    Exception? _error;
    BsonDocument _rootAfterTheFailure;
    BsonDocument _root;
    BsonDocument _other;

    async Task Because()
    {
        // Without a fence the segments are separate writes, so the first can be visible, with its watermark, when the second fails.
        // That window is the same class as the one a join-before-direct order always had. The redelivery must still be admitted.
        using var sink = CreateSink(new FailingFirstJoinWrites());
        Change[] changes = [Direct(("count", 1)), Join(true, ("count", 2))];
        _error = await Catch.Exception(() => sink.ApplyChanges(RootKey, ChangesetOf(changes), 20UL, SinkWriteMode.Always));
        _rootAfterTheFailure = await Read("root");
        await sink.ApplyChanges(RootKey, ChangesetOf(changes), 20UL, SinkWriteMode.Always);
        _root = await Read("root");
        _other = await Read("other");
    }

    [Fact] void should_fail_the_first_apply() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_have_persisted_the_first_segment() => _rootAfterTheFailure["count"].ToInt32().ShouldEqual(1);
    [Fact] void should_have_stamped_the_watermark_with_the_first_segment() => WatermarkOf(_rootAfterTheFailure).ShouldEqual(20);
    [Fact] void should_admit_the_redelivery_and_converge_the_root() => _root["count"].ToInt32().ShouldEqual(2);
    [Fact] void should_converge_the_other_row() => _other["count"].ToInt32().ShouldEqual(2);

    sealed class FailingFirstJoinWrites : ISinkWrites
    {
        bool _failed;

        public int? MaxBatchSize => null;

        public bool IsInBatch => false;

        public Task<T> InBatch<T>(Func<Task<T>> work) => work();

        public void AfterCommit(Action change) => change();

        public Task<UpdateResult> UpdateOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options) =>
            UnfencedSinkWrites.Instance.UpdateOne(collection, filter, update, options);

        public Task<UpdateResult> UpdateMany(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter, UpdateDefinition<BsonDocument> update, UpdateOptions options)
        {
            if (!_failed)
            {
                _failed = true;
                throw new InvalidOperationException("The join write fails once");
            }

            return UnfencedSinkWrites.Instance.UpdateMany(collection, filter, update, options);
        }

        public Task<DeleteResult> DeleteOne(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter) =>
            UnfencedSinkWrites.Instance.DeleteOne(collection, filter);

        public Task<DeleteResult> DeleteMany(IMongoCollection<BsonDocument> collection, FilterDefinition<BsonDocument> filter) =>
            UnfencedSinkWrites.Instance.DeleteMany(collection, filter);

        public Task BulkWrite(IMongoCollection<BsonDocument> collection, IReadOnlyList<WriteModel<BsonDocument>> requests, CancellationToken cancellationToken) =>
            UnfencedSinkWrites.Instance.BulkWrite(collection, requests, cancellationToken);
    }
}
