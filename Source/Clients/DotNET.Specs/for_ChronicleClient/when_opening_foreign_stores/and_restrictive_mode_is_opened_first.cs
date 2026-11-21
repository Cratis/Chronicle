// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class and_restrictive_mode_is_opened_first : when_opening_namespace_stores.given.a_client_with_seeds
{
    IEventStore _restricted;
    IEventStore _inline;
    IEventStore _cached;

    async Task Because()
    {
        _restricted = await _client.GetEventStore("foreign", null, RegistrationMode.AtDeployment);
        _inline = await _client.GetEventStore("foreign");
        _cached = await _client.GetEventStore("foreign", null, RegistrationMode.AtDeployment);
    }

    [Fact] void should_separate_modes() => _restricted.ShouldNotBeSame(_inline);
    [Fact] void should_preserve_restrictive_handle() => _cached.ShouldBeSame(_restricted);
    [Fact] void should_share_transport() => _restricted.Connection.ShouldBeSame(_inline.Connection);
    [Fact] void should_still_seed_inline_handle() => _requests.Count.ShouldEqual(1);
}
