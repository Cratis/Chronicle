// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler;

public class when_the_immediate_peer_was_not_recorded : given.a_trusted_proxy_handler
{
    void Establish()
    {
        _context.Features.Set<IImmediatePeerFeature>(null);
        _context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(ProxyAddress);
    }

    Task Because() => Authenticate();

    [Fact] void should_fail_rather_than_trust_the_rewritable_remote_address() => _result.Failure.ShouldNotBeNull();
}
