// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.given;

public class an_observer_state_storage : Specification
{
    protected ObserverStateStorage _storage;
    protected KernelObserverState _state;
    protected BsonDocument _set;
    protected BsonDocument _update;

    void Establish()
    {
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        var collection = Substitute.For<IMongoCollection<ObserverState>>();
        database.GetObserverStateCollection().Returns(collection);
        collection.UpdateOneAsync(
            Arg.Any<FilterDefinition<ObserverState>>(),
            Arg.Do<UpdateDefinition<ObserverState>>(update =>
            {
                var registry = BsonSerializer.SerializerRegistry;
                _update = update.Render(new RenderArgs<ObserverState>(registry.GetSerializer<ObserverState>(), registry)).AsBsonDocument;
                _set = _update["$set"].AsBsonDocument;
            }),
            Arg.Any<UpdateOptions>(),
            Arg.Any<CancellationToken>()).Returns(new UpdateResult.Acknowledged(1, 1, null));
        _storage = new(database);
        _state = new() { Identifier = "observer" };
    }
}
