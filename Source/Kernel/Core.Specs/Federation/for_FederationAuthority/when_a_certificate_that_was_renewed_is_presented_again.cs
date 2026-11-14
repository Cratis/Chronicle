// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationAuthority;

public class when_a_certificate_that_was_renewed_is_presented_again : given.a_federation_authority
{
    FederationIssuance _first;
    FederationIssuance _second;
    FederationIssuance _fromTheSuperseded;
    X509Certificate2 _original;
    X509Certificate2 _renewed;
    FederationIssuedCertificateState _supersededState;

    async Task Because()
    {
        var code = await Enroll("acme", "prod");
        var (original, originalKey) = await Redeem(code);
        originalKey.Dispose();
        _original = CertificateOf(original);

        _time.Advance(TimeSpan.FromDays(8));
        using var firstKey = NewKey();
        _first = await _authority.Renew(_original.RawData, Csr(firstKey));
        _renewed = CertificateOf(_first);

        // A leaked copy of the certificate that was renewed.
        using var secondKey = NewKey();
        _second = await _authority.Renew(_original.RawData, Csr(secondKey));

        using var thirdKey = NewKey();
        _fromTheSuperseded = await _authority.Renew(_renewed.RawData, Csr(thirdKey));
        _supersededState = await _registry.FindIssuedCertificate(_original.SerialNumber);
    }

    void Destroy()
    {
        _original.Dispose();
        _renewed.Dispose();
    }

    [Fact] void should_have_renewed_the_first_time() => _first.Outcome.ShouldEqual(FederationIssuanceOutcome.Issued);
    [Fact] void should_have_revoked_the_certificate_that_was_renewed() => _supersededState.Revoked.ShouldBeTrue();
    [Fact] void should_refuse_to_renew_the_superseded_certificate_again() => _second.Outcome.ShouldEqual(FederationIssuanceOutcome.Rejected);
    [Fact] void should_still_renew_the_current_certificate() => _fromTheSuperseded.Outcome.ShouldEqual(FederationIssuanceOutcome.Issued);
}
