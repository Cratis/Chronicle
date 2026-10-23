// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverHandledCountsStorage.given;

public class an_observer_handled_counts_storage : Specification
{
    protected static readonly ObserverId _observerId = "observer";
    protected IMongoCollection<ObserverPartitionCounts> _collection;
    protected ObserverHandledCountsStorage _storage;

    void Establish()
    {
        _collection = Substitute.For<IMongoCollection<ObserverPartitionCounts>>();
        var database = Substitute.For<IEventStoreNamespaceDatabase>();
        database.GetCollection<ObserverPartitionCounts>(WellKnownCollectionNames.ObserverHandledCounts).Returns(_collection);
        _storage = new(database);
    }
}
