// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.InFlightEvents.for_InFlightEventsStorage.given;

public class an_in_flight_events_storage : Namespaces.given.a_migrated_namespace_database
{
    protected InFlightEventsStorage _storage;

    void Establish() => _storage = new(_eventStore, _namespace, _database);

    protected InFlightEventsStorage FreshStorage() => new(_eventStore, _namespace, _database);
}
