// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Storage.Changes;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_pipeline_preparation_precedes_a_concurrent_insert(MongoDBFixture fixture) : given.a_prefetch_sink(fixture)
{
    BsonDocument _preparedDocument;
    BsonDocument _baselineDocument;

    async Task Because()
    {
        var projection = Substitute.For<IProjection>();
        projection.HasKeyResolverFor(Arg.Any<EventType>()).Returns(true);
        projection.GetKeyResolverFor(Arg.Any<EventType>()).Returns(new KeyResolvers(NullLogger<KeyResolvers>.Instance).FromEventSourceId);
        var storage = Substitute.For<IEventSequenceStorage, IReplayScopedCache>();
        var pipeline = new ProjectionPipeline(
            projection,
            _sink,
            Substitute.For<IChangesetStorage>(),
            Substitute.For<IObjectComparer>(),
            [new WriteCount(_sink, () => Changes(1))],
            new ProjectionHandleLock(),
            (IReplayScopedCache)storage,
            NullLogger<ProjectionPipeline>.Instance);
        await pipeline.ResumeReplay(new(new("id", 1), "read-models", "read-models-revert", DateTimeOffset.UtcNow));
        await pipeline.BeginBulk();
        var preparedEvent = AppendedEvent.EmptyWithEventSequenceNumber(0UL) with { Context = EventContext.Empty with { EventSourceId = "k", SequenceNumber = 0UL } };
        await pipeline.PreparePage([preparedEvent]);
        (await _sink.FindOrDefault(_key)).ShouldBeNull();
        await _collection.InsertManyAsync(new[] { "k", "baseline" }.Select(id => new BsonDocument { { "_id", id }, { "count", 40 }, { "concurrentField", "retained" }, { "__lastHandledEventSequenceNumber", -1L } }));
        await pipeline.Handle(preparedEvent);
        await pipeline.Handle(preparedEvent with { Context = preparedEvent.Context with { EventSourceId = "baseline" } });
        pipeline.ClearPreparedPage();
        await pipeline.EndBulk();
        _preparedDocument = await _collection.Find(new BsonDocument("_id", "k")).SingleAsync();
        _baselineDocument = await _collection.Find(new BsonDocument("_id", "baseline")).SingleAsync();
        _preparedDocument.Remove("_id");
        _baselineDocument.Remove("_id");
    }

    [Fact] void should_match_the_unprepared_pipeline_result() => _preparedDocument.ShouldEqual(_baselineDocument);
    [Fact] void should_preserve_the_concurrently_inserted_field() => _preparedDocument["concurrentField"].AsString.ShouldEqual("retained");
    [Fact] void should_apply_the_property_update() => _preparedDocument["count"].AsInt32.ShouldEqual(1);

    sealed class WriteCount(ISink sink, Func<Changeset<AppendedEvent, ExpandoObject>> changes) : ICanPerformProjectionPipelineStep
    {
        public async ValueTask<ProjectionEventContext> Perform(IProjection projection, ProjectionEventContext context)
        {
            var resolver = projection.GetKeyResolverFor(context.Event.Context.EventType);
            var key = ((ResolvedKey)await resolver(Substitute.For<IEventSequenceStorage>(), sink, context.Event)).Key;
            await sink.FindOrDefault(key);
            await sink.ApplyChanges(key, changes(), context.Event.Context.SequenceNumber);
            return context with { Key = key };
        }
    }
}
