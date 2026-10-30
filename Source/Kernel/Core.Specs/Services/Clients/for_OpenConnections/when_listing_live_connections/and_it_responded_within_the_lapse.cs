// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_OpenConnections.when_listing_live_connections;

public class and_it_responded_within_the_lapse : given.an_open_connection
{
    IEnumerable<LiveClientConnection> _live;

    void Because()
    {
        Responded(registryKnowsClient: true, TimeSpan.FromSeconds(1));
        _live = Live(TimeSpan.FromSeconds(2));
    }

    [Fact] void should_list_it() => _live.Single().ConnectionId.Value.ShouldEqual(TheConnection);
    [Fact] void should_describe_it_from_the_request() => _live.Single().Version.ShouldEqual("1.0.0");
}
