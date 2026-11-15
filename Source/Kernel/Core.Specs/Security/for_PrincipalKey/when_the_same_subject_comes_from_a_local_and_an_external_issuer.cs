// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security.for_PrincipalKey;

public class when_the_same_subject_comes_from_a_local_and_an_external_issuer : given.principals
{
    readonly string _subject = Guid.NewGuid().ToString();
    PrincipalKey _local;
    PrincipalKey _remote;

    void Because()
    {
        _local = PrincipalKey.From(Principal(System.Security.Claims.ClaimsIdentity.DefaultIssuer, _subject), _internal);
        _remote = PrincipalKey.From(Principal(ExternalIssuer, _subject), _external);
    }

    [Fact] void should_give_different_keys() => _remote.ShouldNotEqual(_local);
    [Fact] void should_give_different_actors() => _remote.ToString().ShouldNotEqual(_local.ToString());
    [Fact] void should_normalize_the_local_issuer() => _local.ToString().ShouldEqual($"chronicle:local|{_subject}");
    [Fact] void should_qualify_the_external_subject_by_its_issuer() => _remote.ToString().ShouldEqual($"{ExternalIssuer}|{_subject}");
}
