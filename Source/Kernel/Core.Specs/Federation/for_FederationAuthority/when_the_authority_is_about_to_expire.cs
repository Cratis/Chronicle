// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_the_authority_is_about_to_expire : given.a_federation_authority
{
    DateTimeOffset _authorityExpires;
    FederationIssuance _beforeExpiry;
    FederationIssuance _afterExpiry;

    async Task Because()
    {
        var now = _time.GetUtcNow();
        _authorityExpires = now.AddDays(3);
        _keys.Replace(FederationCertificateAuthority.CreateAuthority("example", now.AddDays(-1), _authorityExpires));

        // Certificates are issued for 14 days, which is longer than the authority has left.
        var first = await Enroll("acme", "prod");
        (_beforeExpiry, var firstKey) = await Redeem(first);
        firstKey.Dispose();

        _time.Advance(TimeSpan.FromDays(4));
        var second = await Enroll("acme", "test");
        (_afterExpiry, var secondKey) = await Redeem(second);
        secondKey.Dispose();
    }

    [Fact] void should_issue_before_the_authority_expires() => _beforeExpiry.Outcome.ShouldEqual(FederationIssuanceOutcome.Issued);
    [Fact] void should_not_issue_a_certificate_that_outlives_the_authority() => (_beforeExpiry.NotAfter <= _authorityExpires).ShouldBeTrue();
    [Fact] void should_not_issue_once_the_authority_has_expired() => _afterExpiry.ShouldEqual(FederationIssuance.Rejected);
}
