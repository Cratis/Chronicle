// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Security.for_PrincipalKey;

public class when_a_trusted_proxy_user_has_the_subject_of_a_local_user : given.principals
{
    static readonly Authentication _proxied = new() { Http = new HttpAuthentication { Mode = HttpAuthenticationMode.TrustedProxy } };
    readonly string _subject = Guid.NewGuid().ToString();
    PrincipalKey _local;
    PrincipalKey _proxy;

    void Because()
    {
        _local = PrincipalKey.From(Principal(System.Security.Claims.ClaimsIdentity.DefaultIssuer, _subject), _proxied);
        _proxy = PrincipalKey.From(Principal(TrustedProxyAuthentication.DefaultIssuer, _subject), _proxied);
    }

    [Fact] void should_give_different_keys() => _proxy.ShouldNotEqual(_local);
    [Fact] void should_keep_the_proxy_issuer() => _proxy.ToString().ShouldEqual($"{TrustedProxyAuthentication.DefaultIssuer}|{_subject}");
    [Fact] void should_still_normalize_the_local_issuer() => _local.ToString().ShouldEqual($"chronicle:local|{_subject}");
}
