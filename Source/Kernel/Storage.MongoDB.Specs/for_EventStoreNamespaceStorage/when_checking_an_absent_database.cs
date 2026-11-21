// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_EventStoreNamespaceStorage;

public class when_checking_an_absent_database : given.a_namespace_database
{
    bool _hasEvents;

    async Task Because() => _hasEvents = await _database.HasEvents();

    [Fact] void should_report_no_event_data() => _hasEvents.ShouldBeFalse();
    [Fact] void should_not_request_any_collections() => _probes.ShouldBeEmpty();
}
