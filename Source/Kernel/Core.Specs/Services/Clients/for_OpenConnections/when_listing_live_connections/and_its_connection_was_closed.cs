// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_OpenConnections.when_listing_live_connections;

public class and_its_connection_was_closed : given.an_open_connection
{
    IEnumerable<LiveClientConnection> _live;

    void Because()
    {
        Close();
        _live = Live(TimeSpan.FromSeconds(1));
    }

    [Fact] void should_not_list_it() => _live.ShouldBeEmpty();
}
