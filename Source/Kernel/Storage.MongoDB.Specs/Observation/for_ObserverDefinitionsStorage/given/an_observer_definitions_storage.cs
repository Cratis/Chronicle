// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverDefinitionsStorage.given;

public class an_observer_definitions_storage : Specification
{
    protected ObserverDefinitionsStorage _storage;
    protected IEventStoreDatabase _eventStoreDatabase;
    protected IMongoCollection<ObserverDefinition> _collection;

    void Establish()
    {
        _eventStoreDatabase = Substitute.For<IEventStoreDatabase>();
        _collection = Substitute.For<IMongoCollection<ObserverDefinition>>();
        _eventStoreDatabase.GetCollection<ObserverDefinition>(Arg.Any<string>()).Returns(_collection);
        _storage = new ObserverDefinitionsStorage(_eventStoreDatabase);
    }
}
