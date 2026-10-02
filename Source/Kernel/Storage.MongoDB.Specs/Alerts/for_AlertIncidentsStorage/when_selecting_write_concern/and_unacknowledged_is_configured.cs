// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts.for_AlertIncidentsStorage.when_selecting_write_concern;

public class and_unacknowledged_is_configured : given.a_collection
{
    void Establish() => _collection.Settings.Returns(new MongoCollectionSettings { WriteConcern = WriteConcern.Unacknowledged });
    void Because() => _ = new AlertIncidentsStorage(_database);

    [Fact] void should_strengthen_to_acknowledged() => _collection.Received(1).WithWriteConcern(WriteConcern.Acknowledged);
}
