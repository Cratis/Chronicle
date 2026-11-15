// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_CertificatePin;

public class when_matching : given.certificates
{
    bool _sameCertificate;
    bool _otherCertificateMatches;
    bool _roundTripped;

    void Because()
    {
        var pin = CertificatePin.From(_certificate);
        _sameCertificate = pin.Matches(_certificate);
        _otherCertificateMatches = pin.Matches(_otherCertificate);
        _roundTripped = CertificatePin.Parse(pin.ToString()).Matches(_certificate);
    }

    [Fact] void should_match_the_pinned_certificate() => _sameCertificate.ShouldBeTrue();
    [Fact] void should_not_match_another_certificate() => _otherCertificateMatches.ShouldBeFalse();
    [Fact] void should_survive_a_text_round_trip() => _roundTripped.ShouldBeTrue();
}
