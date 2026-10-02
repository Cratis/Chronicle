// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Alerts.for_AlertIncidentsStorage.when_selecting_write_concern;

public class and_majority_is_configured : given.a_collection
{
    void Establish() => _collection.Settings.Returns(new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });
    void Because() => _ = AlertIncidentsStorage.AcknowledgedCollection(_database);

    [Fact] void should_preserve_the_inherited_concern() => _collection.DidNotReceive().WithWriteConcern(Arg.Any<WriteConcern>());
}
