// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_filtering_by_audience.an_operator_addresses_two_lobbies.context;

namespace Cratis.Chronicle.Integration.for_EventStoreSubscriptions.when_filtering_by_audience;

[Collection(ChronicleCollection.Name)]
public class an_operator_addresses_two_lobbies(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.an_operator_addressing_two_lobbies(fixture)
    {
        public IEnumerable<string> LobbyAInbox;
        public IEnumerable<string> LobbyBInbox;

        async Task Because()
        {
            await Publish("to-a", "lobby-a");
            await Publish("to-b", "lobby-b");
            await Publish("untagged");
            var last = await Publish("to-both", "lobby-a", "lobby-b");
            LobbyAInbox = await InboxOf(LobbyA, last);
            LobbyBInbox = await InboxOf(LobbyB, last);
        }
    }

    [Fact] void should_deliver_only_events_for_lobby_a_to_lobby_a() => Context.LobbyAInbox.ShouldContainOnly("to-a", "to-both");
    [Fact] void should_deliver_only_events_for_lobby_b_to_lobby_b() => Context.LobbyBInbox.ShouldContainOnly("to-b", "to-both");
}
