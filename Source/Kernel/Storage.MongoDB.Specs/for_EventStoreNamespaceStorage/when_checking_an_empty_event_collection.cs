// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceStorage;

public class when_checking_an_empty_event_collection : given.a_namespace_database
{
    bool _hasEvents;

    void Establish() => _names.Add(WellKnownCollectionNames.EventLog);

    async Task Because() => _hasEvents = await _database.HasEvents();

    [Fact] void should_not_treat_a_physical_empty_collection_as_event_data() => _hasEvents.ShouldBeFalse();
    [Fact] void should_probe_the_existing_collection() => _probes.ShouldContainOnly(WellKnownCollectionNames.EventLog);
}
