// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_namespace_stores;

public class and_global_and_namespaced_seeds_are_discovered : given.a_client_with_seeds
{
    async Task Because()
    {
        await _client.GetEventStore("current", "Default");
        await _client.GetEventStore("current", "tenant");
    }

    [Fact] void should_send_global_seeds_only_once_for_the_event_store() => _requests.Count(_ => _.GlobalByEventSource.Any()).ShouldEqual(1);
    [Fact] void should_send_namespaced_seeds_from_each_store() => _requests.Count(_ => _.NamespacedEntries.Any()).ShouldEqual(2);
}
