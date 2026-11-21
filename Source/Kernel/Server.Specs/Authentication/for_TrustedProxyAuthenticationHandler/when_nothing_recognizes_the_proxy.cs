// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler;

public class when_nothing_recognizes_the_proxy : given.a_trusted_proxy_handler
{
    void Establish() => _proxy = new();

    Task Because() => Authenticate();

    [Fact] void should_fail_closed() => _result.Failure.ShouldNotBeNull();
}
