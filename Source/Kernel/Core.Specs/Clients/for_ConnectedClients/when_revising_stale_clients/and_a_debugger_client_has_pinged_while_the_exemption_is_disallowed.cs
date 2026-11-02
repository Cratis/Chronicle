// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectedClients.when_revising_stale_clients;

public class and_a_debugger_client_has_pinged_while_the_exemption_is_disallowed : given.a_connected_clients_with_stale_threshold
{
    protected override bool AllowDebuggerExemption => false;

    async Task Establish()
    {
        await Connect(true);
        await _clients.OnClientPing(_connection);
        await MakeStale();
    }

    Task Because() => Revise();

    [Fact] async Task should_be_removed() => (await _clients.IsConnected(_connection)).ShouldEqual(false);
}
