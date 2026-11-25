// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler;

public class when_both_are_configured_and_the_peer_is_untrusted : given.a_trusted_proxy_handler
{
    void Establish()
    {
        _proxy = new() { SharedSecret = "proxy-secret", KnownNetworks = TrustedNetworks };
        _context.Request.Headers["X-Chronicle-Proxy-Secret"] = "proxy-secret";
        FromPeer("203.0.113.9");
    }

    Task Because() => Authenticate();

    [Fact] void should_fail_because_both_must_hold() => _result.Failure.ShouldNotBeNull();
}
