// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts.for_AlertIncidentsStorage.when_selecting_write_concern.given;

public class a_collection : Specification
{
    protected IEventStoreNamespaceDatabase _database;
    protected IMongoCollection<AlertIncidentDocument> _collection;

    void Establish()
    {
        _database = Substitute.For<IEventStoreNamespaceDatabase>();
        _collection = Substitute.For<IMongoCollection<AlertIncidentDocument>>();
        _database.GetCollection<AlertIncidentDocument>(WellKnownCollectionNames.AlertIncidents).Returns(_collection);
        _collection.WithReadPreference(ReadPreference.Primary).Returns(_collection);
        _collection.WithWriteConcern(Arg.Any<WriteConcern>()).Returns(_collection);
    }
}
