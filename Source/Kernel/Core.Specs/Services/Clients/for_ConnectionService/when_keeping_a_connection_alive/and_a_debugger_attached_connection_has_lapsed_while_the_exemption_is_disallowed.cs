// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.when_keeping_a_connection_alive;

public class and_a_debugger_attached_connection_has_lapsed_while_the_exemption_is_disallowed : given.a_connected_client
{
    protected override bool AllowDebuggerExemption => false;

    async Task Because()
    {
        Connect(isRunningWithDebugger: true);
        await WaitUntil(() => KeepAlivesSent >= 1);
        Lapse();
        await WaitUntil(() => Completed);
    }

    [Fact] void should_complete_the_stream() => Completed.ShouldBeTrue();
    [Fact] void should_not_keep_the_client_registered() => CallsTo(nameof(IConnectedClients.OnClientPing)).ShouldEqual(0);
}
