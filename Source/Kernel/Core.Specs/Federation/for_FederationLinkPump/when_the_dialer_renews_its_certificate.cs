// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationLinkPump;

public class when_the_dialer_renews_its_certificate : given.a_dialing_link_pump
{
    async Task Because()
    {
        await _dialPump.Step(CancellationToken.None);
        await Eventually(() => _serve.Calls >= 1);
        _dialClients.Serial = "serial-2";
        await _dialPump.Step(CancellationToken.None);
        await _dialPump.Step(CancellationToken.None);
        await Eventually(() => _serve.Calls >= 2);
    }

    [Fact] void should_reconnect_once_with_the_new_certificate() => _dialClients.Created.ShouldEqual(2);
    [Fact] void should_close_the_connection_that_presented_the_old_one() => _dialClients.Closed.ShouldEqual(1);
    [Fact] void should_open_the_serve_call_again_on_the_new_connection() => _serve.Calls.ShouldEqual(2);
}
