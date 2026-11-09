// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;

namespace Cratis.Chronicle.Federation.for_FederationPinValidator;

public class when_validating_a_server_certificate : given.certificates
{
    RemoteCertificateValidationCallback _callback;
    bool _matching;
    bool _matchingDespiteChainAndNameErrors;
    bool _otherEvenThoughChainIsValid;
    bool _missing;
    Exception _malformedPin;

    void Establish() => _callback = FederationPinValidator.CreateCallback(CertificatePin.From(_certificate).ToString());

    void Because()
    {
        _matching = _callback(this, _certificate, null, SslPolicyErrors.None);
        _matchingDespiteChainAndNameErrors = _callback(this, _certificate, null, SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch);
        _otherEvenThoughChainIsValid = _callback(this, _otherCertificate, null, SslPolicyErrors.None);
        _missing = _callback(this, null, null, SslPolicyErrors.RemoteCertificateNotAvailable);
        _malformedPin = Catch.Exception(() => FederationPinValidator.CreateCallback("not-a-pin"));
    }

    [Fact] void should_accept_the_certificate_holding_the_pinned_key() => _matching.ShouldBeTrue();
    [Fact] void should_not_care_about_chain_or_name_errors() => _matchingDespiteChainAndNameErrors.ShouldBeTrue();
    [Fact] void should_reject_another_certificate_even_when_the_chain_is_valid() => _otherEvenThoughChainIsValid.ShouldBeFalse();
    [Fact] void should_reject_a_missing_certificate() => _missing.ShouldBeFalse();
    [Fact] void should_refuse_a_malformed_pin() => _malformedPin.ShouldBeOfExactType<FormatException>();
}
