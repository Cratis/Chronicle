// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_EventStoreNamespaceStorage;

public class when_the_last_event_is_removed : given.a_namespace_database
{
    bool _before;
    bool _after;

    Task Establish() => Execute(NamespaceConnection, "CREATE TABLE \"event-log\" (sequence_number INTEGER); INSERT INTO \"event-log\" VALUES (0);");

    async Task Because()
    {
        _before = await HasData();
        await Execute(NamespaceConnection, "DELETE FROM \"event-log\";");
        _after = await HasData();
    }

    [Fact] void should_initially_find_the_log_event() => _before.ShouldBeTrue();
    [Fact] void should_not_keep_a_sticky_event_flag() => _after.ShouldBeFalse();
}
