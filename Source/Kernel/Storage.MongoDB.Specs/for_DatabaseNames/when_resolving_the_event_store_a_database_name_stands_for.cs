// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.MongoDB.for_DatabaseNames;

public class when_resolving_the_event_store_a_database_name_stands_for : Specification
{
    EventStoreName[] _eventStores;

    void Because() => _eventStores =
    [
        DatabaseNames.EventStoreFor("_system"),
        DatabaseNames.EventStoreFor("chronicle-system", "chronicle-system"),
        DatabaseNames.EventStoreFor("Ada")
    ];

    [Fact] void should_resolve_the_default_system_database_name_to_the_system_event_store() => _eventStores[0].ShouldEqual(EventStoreName.System);
    [Fact] void should_resolve_a_configured_system_database_name_to_the_system_event_store() => _eventStores[1].ShouldEqual(EventStoreName.System);
    [Fact] void should_resolve_any_other_name_to_the_event_store_of_that_name() => _eventStores[2].ShouldEqual(new EventStoreName("Ada"));
}
