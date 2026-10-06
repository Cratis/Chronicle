// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Alerts.for_AlertIncidentsStorage.when_constructing;

public class and_the_database_is_not_used_yet : Specification
{
    IEventStoreNamespaceDatabase _database;

    void Establish() => _database = Substitute.For<IEventStoreNamespaceDatabase>();

    void Because() => _ = new AlertIncidentsStorage(_database);

    [Fact] void should_not_resolve_the_collection() => _database.DidNotReceive().GetCollection<AlertIncidentDocument>(Arg.Any<string>());
}
