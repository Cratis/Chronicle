// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.ReadModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_beginning_replay;

public class and_namespaces_share_a_global_sink : given.a_projection_replay_handler_with_projection
{
    SinkCollections _sharedCollections;
    IMongoCollection<BsonDocument> _liveCollection;
    IMongoCollection<BsonDocument> _replayCollection;
    ObserverDetails _secondNamespace;
    ReplayContext _context;

    void Establish()
    {
        _projection.Scope.Returns(ProjectionScope.Global);
        _secondNamespace = _observerDetails with { Key = _observerDetails.Key with { Namespace = "OtherNamespace" } };
        _projections.GetCurrent(_secondNamespace.Key.EventStore, _secondNamespace.Key.Namespace, _secondNamespace.Key.ObserverId).Returns(_projection);
        _projectionPipelineManager.GetFor(_secondNamespace.Key.EventStore, _secondNamespace.Key.Namespace, _projection).Returns(_projectionPipeline);
        _context = new(_readModelType, _readModelName, "previous", DateTimeOffset.UtcNow);
        _replayContexts.Establish(_readModelType, _readModelName).Returns(_context);

        var database = Substitute.For<IMongoDatabase>();
        _liveCollection = Substitute.For<IMongoCollection<BsonDocument>>();
        _replayCollection = Substitute.For<IMongoCollection<BsonDocument>>();
        database.GetCollection<BsonDocument>(_readModelName.Value).Returns(_liveCollection);
        database.GetCollection<BsonDocument>($"replay-{_readModelName}").Returns(_replayCollection);
        _sharedCollections = new(_readModel, database);

        // Both namespace pipelines hold this sink; use the real collection routing and clearing code.
        _projectionPipeline.BeginReplay(Arg.Any<ReplayContext>())
            .Returns(call => _sharedCollections.BeginReplay(call.Arg<ReplayContext>()));
    }

    async Task Because()
    {
        await _handler.BeginReplayFor(_observerDetails);
        await _handler.BeginReplayFor(_secondNamespace);
    }

    [Fact] void should_leave_other_namespaces_live_writes_on_the_live_collection() => _sharedCollections.GetCollection().ShouldBeSame(_liveCollection);
    [Fact] async Task should_not_clear_a_shared_rebuild() => await _replayCollection.DidNotReceive().DeleteManyAsync(Arg.Any<FilterDefinition<BsonDocument>>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_not_establish_a_namespace_local_replay_context() => await _replayContexts.DidNotReceive().Establish(Arg.Any<ReadModelType>(), Arg.Any<ReadModelContainerName>());
}
