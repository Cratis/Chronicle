// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_redeeming_an_enrollment_twice : given.a_federation_authority
{
    FederationIssuance _first;
    FederationIssuance _second;

    async Task Because()
    {
        var code = await Enroll();
        (_first, var firstKey) = await Redeem(code);
        (_second, var secondKey) = await Redeem(code);
        firstKey.Dispose();
        secondKey.Dispose();
    }

    [Fact] void should_issue_for_the_first() => _first.Outcome.ShouldEqual(FederationIssuanceOutcome.Issued);
    [Fact] void should_refuse_the_replay() => _second.ShouldEqual(FederationIssuance.Rejected);
    [Fact] void should_have_issued_exactly_one_certificate() => _registry.Appended.Count(_ => _.Event is FederationCertificateIssued).ShouldEqual(1);
}
