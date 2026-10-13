// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_namespace_stores;

public class and_the_connection_is_reestablished : given.a_client_with_seeds
{
    async Task Establish()
    {
        await _client.GetEventStore("current", "Default");
        await _client.GetEventStore("current", "tenant");
    }

    async Task Because()
    {
        await _connection.Lifecycle.Disconnected();
        await _connection.Lifecycle.Connected();
        await _client.GetEventStore("current", "another-tenant");
    }

    [Fact] void should_send_globals_once_again_on_the_new_connection() => _requests.Count(_ => _.GlobalByEventSource.Any()).ShouldEqual(2);
    [Fact] void should_still_send_the_new_stores_namespaced_entries() => _requests.Count(_ => _.NamespacedEntries.Any()).ShouldEqual(3);
}
