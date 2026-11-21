// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.Authentication.for_AuthenticationModeResponse;

public class when_creating_from_the_configuration : Specification
{
    [Fact] void should_report_chronicle_by_default() => AuthenticationModeResponse.From(new()).ShouldEqual(new AuthenticationModeResponse("chronicle", null));

    [Fact]
    void should_report_the_trusted_proxy_and_its_sign_out_url() =>
        AuthenticationModeResponse.From(new() { Mode = HttpAuthenticationMode.TrustedProxy, TrustedProxy = new() { SignOutUrl = "/oauth2/sign_out" } })
            .ShouldEqual(new AuthenticationModeResponse("trustedProxy", "/oauth2/sign_out"));

    [Fact]
    void should_never_report_the_sign_out_url_of_an_inactive_proxy() =>
        AuthenticationModeResponse.From(new() { TrustedProxy = new() { SignOutUrl = "/oauth2/sign_out" } }).SignOutUrl.ShouldBeNull();
}
