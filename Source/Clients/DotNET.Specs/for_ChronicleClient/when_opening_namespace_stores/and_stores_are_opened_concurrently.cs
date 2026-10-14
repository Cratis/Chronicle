// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_namespace_stores;

public class and_stores_are_opened_concurrently : given.a_client_with_seeds
{
    async Task Because() => await Task.WhenAll(
        _client.GetEventStore("current", "Default"),
        _client.GetEventStore("current", "tenant"));

    [Fact] void should_send_globals_once() => _requests.Count(_ => _.GlobalByEventSource.Any()).ShouldEqual(1);
    [Fact] void should_send_each_stores_namespaced_entries() => _requests.Count(_ => _.NamespacedEntries.Any()).ShouldEqual(2);
}
