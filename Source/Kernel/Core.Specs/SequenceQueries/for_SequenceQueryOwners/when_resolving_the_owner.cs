// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;

namespace Cratis.Chronicle.SequenceQueries.for_SequenceQueryOwners;

/// <summary>
/// Pins the stored owner format. Saved queries and folders are matched by exact owner value, so a change to
/// this format has to come with a migration of the stored owners.
/// </summary>
public class when_resolving_the_owner : Specification
{
    const string Issuer = "https://identity.example";
    string _fromSubject;
    string _fromExternalSubject;
    string _fromName;
    string _anonymous;

    void Because()
    {
        _fromSubject = SequenceQueryOwners.GetCurrent(Accessor(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "the-subject")], "spec"))));
        _fromExternalSubject = SequenceQueryOwners.GetCurrent(Accessor(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "the-subject", ClaimValueTypes.String, Issuer)], "spec"))));
        _fromName = SequenceQueryOwners.GetCurrent(Accessor(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "the-name")], "spec", ClaimTypes.Name, ClaimTypes.Role))));
        _anonymous = SequenceQueryOwners.GetCurrent(Accessor(null));
    }

    [Fact] void should_use_the_bare_subject() => _fromSubject.ShouldEqual("the-subject");
    [Fact] void should_not_qualify_the_subject_by_its_issuer() => _fromExternalSubject.ShouldEqual("the-subject");
    [Fact] void should_use_the_identity_name_without_a_subject() => _fromName.ShouldEqual("the-name");
    [Fact] void should_use_the_anonymous_owner_without_a_principal() => _anonymous.ShouldEqual("anonymous");

    static ICurrentPrincipalAccessor Accessor(ClaimsPrincipal? principal)
    {
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(principal);
        return accessor;
    }
}
