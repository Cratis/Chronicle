// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_OpenConnections.when_listing_live_connections;

public class and_it_runs_with_a_debugger : given.an_open_connection
{
    IEnumerable<LiveClientConnection> _live;

    void Because()
    {
        OpenWithDebugger();
        Responded(registryKnowsClient: false, _lapse);
        _live = Live(_lapse * 3);
    }

    [Fact] void should_list_it() => _live.Single().ConnectionId.Value.ShouldEqual(TheConnection);
}
