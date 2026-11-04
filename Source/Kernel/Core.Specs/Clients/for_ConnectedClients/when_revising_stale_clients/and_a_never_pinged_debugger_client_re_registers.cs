// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_revising_stale_clients;

public class and_a_never_pinged_debugger_client_re_registers : given.a_connected_clients_with_stale_threshold
{
    async Task Establish()
    {
        await Connect(true);
        await MakeStale();
    }

    async Task Because()
    {
        await Connect(true);
        await Revise();
    }

    [Fact] async Task should_keep_the_fresh_entry() => (await _clients.IsConnected(_connection)).ShouldEqual(true);
}
