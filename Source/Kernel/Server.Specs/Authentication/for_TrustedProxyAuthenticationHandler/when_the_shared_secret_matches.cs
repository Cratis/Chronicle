// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler;

public class when_the_shared_secret_matches : given.a_trusted_proxy_handler
{
    void Establish()
    {
        _proxy = new() { SharedSecret = "proxy-secret" };
        _context.Request.Headers["X-Chronicle-Proxy-Secret"] = "proxy-secret";
    }

    Task Because() => Authenticate();

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
}
