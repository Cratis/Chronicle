// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler;

public class when_an_untrusted_peer_sends_identity_headers : given.a_trusted_proxy_handler
{
    void Establish() => FromPeer("203.0.113.9");

    Task Because() => Authenticate();

    [Fact] void should_fail() => _result.Failure.ShouldNotBeNull();
    [Fact] void should_not_produce_a_principal() => _result.Principal.ShouldBeNull();
}
