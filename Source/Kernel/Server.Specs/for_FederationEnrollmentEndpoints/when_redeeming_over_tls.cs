// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Federation;

namespace Cratis.Chronicle.Server.for_FederationEnrollmentEndpoints;

public class when_redeeming_over_tls : given.a_listening_authority
{
    FederationEnrollmentTransport _transport;
    X509Certificate2 _fetched;
    X509Certificate2 _fetchedWithTheWrongFingerprint;
    FederationIssuance _issued;
    FederationIssuance _unknownEnrollment;
    Exception _unexpectedAuthority;
    X509Certificate2 _otherAuthority;

    void Establish() => _transport = new(TimeProvider.System);

    async Task Because()
    {
        if (!given.Tls13.Supported)
        {
            return;
        }

        _fetched = await _transport.GetAuthority(_endpoint, _fingerprint, CancellationToken.None);
        _fetchedWithTheWrongFingerprint = await _transport.GetAuthority(_endpoint, new string('a', 64), CancellationToken.None);

        _issued = await _transport.Redeem(_endpoint, _fetched, new("known", "secret", Convert.ToBase64String(NewCsr())), CancellationToken.None);
        _unknownEnrollment = await _transport.Redeem(_endpoint, _fetched, new("unknown", "secret", Convert.ToBase64String(NewCsr())), CancellationToken.None);

        // A client that was told to trust another authority must refuse this server.
        _otherAuthority = FederationCertificateAuthority.CreateAuthority("example", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        _unexpectedAuthority = await Catch.Exception(() => _transport.Redeem(_endpoint, _otherAuthority, new("known", "secret", Convert.ToBase64String(NewCsr())), CancellationToken.None));
    }

    void Destroy()
    {
        _fetched?.Dispose();
        _otherAuthority?.Dispose();
    }

    [given.Tls13Fact] void should_serve_the_authority_certificate_without_a_client_certificate() => _fetched.ShouldNotBeNull();
    [given.Tls13Fact] void should_serve_the_certificate_the_code_names() => FederationCertificateAuthority.Fingerprint(_fetched).ShouldEqual(_fingerprint);
    [given.Tls13Fact] void should_not_accept_a_certificate_that_does_not_match_the_fingerprint() => _fetchedWithTheWrongFingerprint.ShouldBeNull();
    [given.Tls13Fact] void should_issue_a_certificate_to_a_peer_without_a_client_certificate() => _issued.Outcome.ShouldEqual(FederationIssuanceOutcome.Issued);
    [given.Tls13Fact] void should_issue_one_that_chains_to_the_authority() => FederationCertificateAuthority.ChainsTo(X509CertificateLoader.LoadCertificate(Convert.FromBase64String(_issued.Certificate)), _authority, DateTimeOffset.UtcNow).ShouldBeTrue();
    [given.Tls13Fact] void should_refuse_an_unknown_enrollment_with_the_generic_answer() => _unknownEnrollment.Outcome.ShouldEqual(FederationIssuanceOutcome.Rejected);
    [given.Tls13Fact] void should_not_let_a_client_trust_a_server_that_does_not_chain_to_its_authority() => _unexpectedAuthority.ShouldNotBeNull();
}
