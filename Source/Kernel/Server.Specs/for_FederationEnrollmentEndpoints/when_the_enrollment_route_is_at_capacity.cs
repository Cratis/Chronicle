// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Authentication;
using Cratis.Chronicle.Federation;

namespace Cratis.Chronicle.Server.for_FederationEnrollmentEndpoints;

public class when_the_enrollment_route_is_at_capacity : given.a_listening_authority
{
    HttpStatusCode _overTheCap;
    HttpStatusCode _first;
    HttpStatusCode _afterItFinished;

    protected override int MaxConcurrent => 1;

    async Task Because()
    {
        if (!given.Tls13.Supported)
        {
            return;
        }

        var gate = new TaskCompletionSource<FederationIssuance>();
        _grain.Redeem("known", "secret", Arg.Any<byte[]>()).Returns(_ => gate.Task);

        var client = CreateClient();
        var url = new Uri(_endpoint, FederationEnrollmentRoutes.Redeem);
        var firstRequest = client.PostAsJsonAsync(url, new FederationRedeemRequest("known", "secret", Convert.ToBase64String(NewCsr())));

        // The first request is now inside the route, waiting for the grain.
        while (!_grain.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == nameof(IFederationAuthority.Redeem)))
        {
            await Task.Delay(10);
        }

        _overTheCap = (await client.PostAsJsonAsync(url, new FederationRedeemRequest("unknown", "secret", Convert.ToBase64String(NewCsr())))).StatusCode;

        gate.SetResult(FederationIssuance.Rejected);
        _first = (await firstRequest).StatusCode;
        _afterItFinished = (await client.PostAsJsonAsync(url, new FederationRedeemRequest("unknown", "secret", Convert.ToBase64String(NewCsr())))).StatusCode;
        client.Dispose();
    }

    [given.Tls13Fact] void should_refuse_beyond_the_cap_without_waiting() => _overTheCap.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [given.Tls13Fact] void should_serve_the_request_that_was_inside() => _first.ShouldEqual(HttpStatusCode.Forbidden);
    [given.Tls13Fact] void should_serve_again_when_it_has_finished() => _afterItFinished.ShouldEqual(HttpStatusCode.Forbidden);

    HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler();
#pragma warning disable CA5398 // Federation deliberately accepts TLS 1.3 only.
        handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls13;
#pragma warning restore CA5398
        handler.SslOptions.RemoteCertificateValidationCallback = (_, certificate, _, _) =>
            certificate is not null && FederationCertificateAuthority.ChainsTo(new(certificate), _authority, DateTimeOffset.UtcNow, FederationCertificateAuthority.ServerAuthentication);
        return new HttpClient(handler) { DefaultRequestVersion = HttpVersion.Version20, DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact };
    }
}
