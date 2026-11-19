// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler;

public class when_a_grpc_call_carries_identity_headers : given.a_trusted_proxy_handler
{
    void Establish() => _context.Request.ContentType = "application/grpc";

    Task Because() => Authenticate();

    [Fact] void should_not_authenticate() => _result.None.ShouldBeTrue();
}
