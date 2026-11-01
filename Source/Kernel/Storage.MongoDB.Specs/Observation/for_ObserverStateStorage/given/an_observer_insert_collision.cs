// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.for_ColdStartStorage.given;
using MongoDB.Driver;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateStorage.given;

public class an_observer_insert_collision : Specification
{
    protected ObserverStateStorage _storage;
    protected IMongoCollection<ObserverState> _collection;
    protected FilterDefinition<ObserverState> _filter;
    protected UpdateDefinition<ObserverState> _update;
    protected bool _accepted;

    void Establish()
    {
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        _collection = Substitute.For<IMongoCollection<ObserverState>>();
        database.GetObserverStateCollection().Returns(_collection);
        _collection.UpdateOneAsync(
            Arg.Do<FilterDefinition<ObserverState>>(filter => _filter = filter),
            Arg.Do<UpdateDefinition<ObserverState>>(update => _update = update),
            Arg.Is<UpdateOptions>(options => options.IsUpsert),
            Arg.Any<CancellationToken>()).ThrowsAsync(duplicate_keys.Duplicate());
        _storage = new(database);
    }
}
