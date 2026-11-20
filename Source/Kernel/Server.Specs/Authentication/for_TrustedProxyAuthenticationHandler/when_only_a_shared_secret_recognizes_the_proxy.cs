// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler;

public class when_only_a_shared_secret_recognizes_the_proxy : given.a_trusted_proxy_handler
{
    void Establish()
    {
        _proxy = new() { SharedSecret = "proxy-secret" };
        _context.Request.Headers["X-Chronicle-Proxy-Secret"] = "proxy-secret";
        FromPeer("203.0.113.9");
    }

    Task Because() => Authenticate();

    [Fact] void should_succeed_wherever_the_proxy_connects_from() => _result.Succeeded.ShouldBeTrue();
}
