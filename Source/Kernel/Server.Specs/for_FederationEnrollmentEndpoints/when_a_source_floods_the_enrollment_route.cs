// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Authentication;
using Cratis.Chronicle.Federation;

namespace Cratis.Chronicle.Server.for_FederationEnrollmentEndpoints;

public class when_a_source_floods_the_enrollment_route : given.a_listening_authority
{
    HttpStatusCode _wrongSecret;
    HttpStatusCode _afterTheLimit;
    HttpStatusCode _oversized;
    HttpStatusCode _notJson;
    HttpStatusCode _renewWithoutCertificate;

    protected override int RequestsPerMinute => 6;

    async Task Because()
    {
        if (!given.Tls13.Supported)
        {
            return;
        }

        using var client = CreateClient();
        var url = new Uri(_endpoint, FederationEnrollmentRoutes.Redeem);

        // Too big for the pre-authentication surface: the body limit is a few kilobytes.
        _oversized = (await client.PostAsJsonAsync(url, new FederationRedeemRequest("x", "y", new string('A', 200_000)))).StatusCode;
        _notJson = (await client.PostAsync(url, new StringContent("not json", System.Text.Encoding.UTF8, "application/json"))).StatusCode;
        _renewWithoutCertificate = (await client.PostAsJsonAsync(new Uri(_endpoint, FederationEnrollmentRoutes.Renew), new FederationRenewRequest(Convert.ToBase64String(NewCsr())))).StatusCode;
        _wrongSecret = (await client.PostAsJsonAsync(url, new FederationRedeemRequest("unknown", "secret", Convert.ToBase64String(NewCsr())))).StatusCode;

        for (var i = 0; i < 10; i++)
        {
            _afterTheLimit = (await client.PostAsJsonAsync(url, new FederationRedeemRequest("unknown", "secret", Convert.ToBase64String(NewCsr())))).StatusCode;
        }
    }

    [given.Tls13Fact] void should_refuse_a_body_over_the_size_limit() => _oversized.ShouldNotEqual(HttpStatusCode.OK);
    [given.Tls13Fact] void should_refuse_a_body_that_is_not_json() => _notJson.ShouldEqual(HttpStatusCode.BadRequest);
    [given.Tls13Fact] void should_require_a_client_certificate_to_renew() => _renewWithoutCertificate.ShouldEqual(HttpStatusCode.Unauthorized);
    [given.Tls13Fact] void should_answer_a_wrong_secret_with_one_generic_refusal() => _wrongSecret.ShouldEqual(HttpStatusCode.Forbidden);
    [given.Tls13Fact] void should_rate_limit_the_source() => _afterTheLimit.ShouldEqual(HttpStatusCode.TooManyRequests);

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
