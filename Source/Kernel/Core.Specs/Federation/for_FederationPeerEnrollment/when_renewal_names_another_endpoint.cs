// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationPeerEnrollment;

public class when_renewal_names_another_endpoint : given.a_peer_enrollment
{
    string _original;
    string _afterPlainHttp;
    string _afterHttps;

    async Task Because()
    {
        await _peer.Redeem((await _authority.CreateEnrollment("acme", "prod")).Code);
        _original = (await _identities.Get()).Endpoint;

        var transport = new MovingTransport(_transport) { Moved = "http://moved.example.com:35100/" };
        using var peer = new FederationPeerEnrollment(_identities, transport, _time, _logger);

        _time.Advance(TimeSpan.FromDays(8));
        await peer.RenewIfDue();
        _afterPlainHttp = (await _identities.Get()).Endpoint;

        transport.Moved = "https://moved.example.com:35100/";
        _time.Advance(TimeSpan.FromDays(8));
        await peer.RenewIfDue();
        _afterHttps = (await _identities.Get()).Endpoint;
    }

    [Fact] void should_ignore_an_endpoint_that_is_not_https() => _afterPlainHttp.ShouldEqual(_original);
    [Fact] void should_follow_an_https_endpoint() => _afterHttps.ShouldEqual("https://moved.example.com:35100/");

    sealed class MovingTransport(IFederationEnrollmentTransport inner) : IFederationEnrollmentTransport
    {
        public string Moved { get; set; } = string.Empty;

        public Task<X509Certificate2?> GetAuthority(Uri endpoint, string authorityFingerprint, CancellationToken cancellationToken) =>
            inner.GetAuthority(endpoint, authorityFingerprint, cancellationToken);

        public Task<FederationIssuance> Redeem(Uri endpoint, X509Certificate2 authority, FederationRedeemRequest request, CancellationToken cancellationToken) =>
            inner.Redeem(endpoint, authority, request, cancellationToken);

        public async Task<FederationIssuance> Renew(Uri endpoint, X509Certificate2 authority, X509Certificate2 clientCertificate, FederationRenewRequest request, CancellationToken cancellationToken) =>
            (await inner.Renew(endpoint, authority, clientCertificate, request, cancellationToken)) with { Endpoint = Moved };
    }
}
