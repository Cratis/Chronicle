// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.Server.Authentication.for_TrustedProxyAuthenticationHandler;

public class when_a_trusted_proxy_forwards_a_user : given.a_trusted_proxy_handler
{
    Task Because() => Authenticate();

    [Fact] void should_succeed() => _result.Succeeded.ShouldBeTrue();
    [Fact] void should_mark_the_identity_as_proxy_authenticated() => TrustedProxyUsers.Is(_result.Principal).ShouldBeTrue();
    [Fact] void should_use_the_user_as_subject() => _result.Principal!.FindFirst("sub")!.Value.ShouldEqual("alice");
    [Fact] void should_give_the_subject_the_proxy_issuer() => _result.Principal!.FindFirst("sub")!.Issuer.ShouldEqual(TrustedProxyAuthentication.DefaultIssuer);
    [Fact] void should_carry_the_email() => _result.Principal!.FindFirst("email")!.Value.ShouldEqual("alice@example.com");
    [Fact] void should_name_the_user() => _result.Principal!.Identity!.Name.ShouldEqual("alice");
    [Fact] void should_carry_each_group() => _result.Principal!.FindAll(TrustedProxyUsers.GroupClaimType).Select(_ => _.Value).ShouldContainOnly(["operators", "chronicle-admins"]);
}
