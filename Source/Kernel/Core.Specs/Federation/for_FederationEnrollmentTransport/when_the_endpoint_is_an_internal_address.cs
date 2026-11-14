// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentTransport;

public class when_the_endpoint_is_an_internal_address : Specification
{
    TcpListener _listener;
    X509Certificate2? _authority;
    FederationIssuance _redeemed;
    bool _connected;

    void Establish()
    {
        _listener = new(IPAddress.Loopback, 0);
        _listener.Start();
    }

    void Destroy() => _listener.Stop();

    async Task Because()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        var options = Options.Create(new ChronicleOptions { Federation = new() { Enrollment = new() { FailureDuration = TimeSpan.Zero } } });
        var transport = new FederationEnrollmentTransport(TimeProvider.System, new FederationEndpointPolicy(options, environment), options);
        var endpoint = new Uri($"https://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");
        using var authorityCertificate = FederationCertificateAuthority.CreateAuthority("example", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        _authority = await transport.GetAuthority(endpoint, FederationCertificateAuthority.Fingerprint(authorityCertificate), CancellationToken.None);
        _redeemed = await transport.Redeem(endpoint, authorityCertificate, new("id", "secret", "AAAA"), CancellationToken.None);
        _connected = _listener.Pending();
    }

    [Fact] void should_not_reach_for_the_authority() => _authority.ShouldBeNull();
    [Fact] void should_refuse_the_redemption_the_same_way_as_any_other_failure() => _redeemed.ShouldEqual(FederationIssuance.Rejected);
    [Fact] void should_never_connect_to_the_address() => _connected.ShouldBeFalse();
}
