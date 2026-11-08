// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Federation.for_FederationPeerAuthenticator;

public class when_authenticating : given.certificates
{
    FederationLink _first;
    FederationLink _revoked;
    FederationPeerAuthenticator _authenticator;
    FederationLink _matchedFirst;
    FederationLink _matchedRevoked;
    FederationLink _matchedUnknown;
    FederationLink _matchedNone;
    FederationPeerAuthenticator _empty;
    FederationLink _matchedWhenEmpty;
    System.Security.Cryptography.X509Certificates.X509Certificate2 _revokedCertificate;
    System.Security.Cryptography.X509Certificates.X509Certificate2 _unknownCertificate;

    void Establish()
    {
        _revokedCertificate = Create("revoked");
        _unknownCertificate = Create("unknown");
        _first = new("first", CertificatePin.From(_certificate), FederationGrant.AllPublicEventTypes);
        _revoked = new("revoked", CertificatePin.From(_revokedCertificate), FederationGrant.AllPublicEventTypes, true);
        _authenticator = new([_first, _revoked]);
        _empty = new([]);
    }

    void Because()
    {
        _matchedFirst = _authenticator.Authenticate(_certificate);
        _matchedRevoked = _authenticator.Authenticate(_revokedCertificate);
        _matchedUnknown = _authenticator.Authenticate(_unknownCertificate);
        _matchedNone = _authenticator.Authenticate(null);
        _matchedWhenEmpty = _empty.Authenticate(_certificate);
    }

    [Fact] void should_authenticate_the_pinned_peer() => _matchedFirst.ShouldEqual(_first);
    [Fact] void should_reject_a_revoked_link() => _matchedRevoked.ShouldBeNull();
    [Fact] void should_not_affect_other_links_when_one_is_revoked() => _matchedFirst.ShouldNotBeNull();
    [Fact] void should_reject_a_certificate_that_is_not_pinned() => _matchedUnknown.ShouldBeNull();
    [Fact] void should_reject_a_missing_certificate() => _matchedNone.ShouldBeNull();
    [Fact] void should_reject_everything_when_no_links_exist() => _matchedWhenEmpty.ShouldBeNull();
}
