// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.for_FederationPeerAdmission;

public class when_the_certificate_is_a_server_certificate : given.a_peer_admission
{
    X509Certificate2 _server;

    async Task Because()
    {
        await Issued("acme", "prod");
        await SetPolicy("acme");
        using var key = NewKey();
        var now = _time.GetUtcNow();

        // The authority signed it, and it even names a valid identity, but it is for authenticating a server.
        _server = FederationCertificateAuthority.IssueServer(_keys.Authority, Csr(key), "spiffe-lookalike.example.com", now.AddMinutes(-1), now.AddDays(1));
        _admitted = await Admit(_server);
    }

    void Destroy() => _server.Dispose();

    [Fact] void should_refuse_the_peer() => _admitted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_say_it_does_not_chain_for_a_client() => _admitted.Refusal.ShouldEqual(FederationAdmissionRefusal.NotChained);
}
