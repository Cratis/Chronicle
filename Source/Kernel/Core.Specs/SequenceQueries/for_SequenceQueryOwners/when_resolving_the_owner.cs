// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.SequenceQueries.for_SequenceQueryOwners;

public class when_resolving_the_owner : Specification
{
    const string ExternalIssuer = "https://identity.example";
    readonly string _subject = Guid.NewGuid().ToString();
    string _local;
    string _remote;
    string _anonymous;

    void Because()
    {
        _local = Owner(ClaimsIdentity.DefaultIssuer, new Authentication());
        _remote = Owner(ExternalIssuer, new Authentication { Authority = ExternalIssuer });
        _anonymous = SequenceQueryOwners.GetCurrent(Accessor(null), new Authentication());
    }

    [Fact] void should_keep_the_raw_subject_for_a_local_principal() => _local.ShouldEqual(_subject);
    [Fact] void should_qualify_an_external_principal_by_its_issuer() => _remote.ShouldEqual($"{ExternalIssuer}|{_subject}");
    [Fact] void should_not_resolve_the_external_principal_to_the_local_owner() => _remote.ShouldNotEqual(_local);
    [Fact] void should_use_the_anonymous_owner_without_a_principal() => _anonymous.ShouldEqual(SequenceQueryOwners.Anonymous);

    static ICurrentPrincipalAccessor Accessor(ClaimsPrincipal? principal)
    {
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        return accessor;
    }

    string Owner(string issuer, Authentication authentication) =>
        SequenceQueryOwners.GetCurrent(Accessor(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", _subject, ClaimValueTypes.String, issuer)], "spec"))), authentication);
}
