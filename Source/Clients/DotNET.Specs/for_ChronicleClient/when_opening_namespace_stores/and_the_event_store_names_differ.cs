// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_namespace_stores;

public class and_the_event_store_names_differ : given.a_client_with_seeds
{
    async Task Because()
    {
        await _client.GetEventStore("current", "Default");
        await _client.GetEventStore("other", "Default");
    }

    [Fact] void should_send_globals_to_each_event_store() => _requests.Where(_ => _.GlobalByEventSource.Any()).Select(_ => _.EventStore).ShouldContainOnly("current", "other");
}
