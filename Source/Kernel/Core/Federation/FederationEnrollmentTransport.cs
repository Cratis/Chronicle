// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationEnrollmentTransport"/> over HTTP/2 and TLS 1.3.
/// </summary>
/// <param name="timeProvider">The <see cref="TimeProvider"/> to check certificate validity with.</param>
[Singleton]
public class FederationEnrollmentTransport(TimeProvider timeProvider) : IFederationEnrollmentTransport
{
    static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public async Task<X509Certificate2?> GetAuthority(Uri endpoint, string authorityFingerprint, CancellationToken cancellationToken)
    {
        // The response is public data that is accepted only if it matches the fingerprint in the code, so the server is not validated here.
#pragma warning disable CA5359 // See above: the response is checked against the fingerprint instead.
        using var handler = CreateHandler((_, _, _, _) => true, null);
#pragma warning restore CA5359
        using var client = new HttpClient(handler, false) { Timeout = _timeout };
        using var message = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint, FederationEnrollmentRoutes.Authority))
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        using var response = await client.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (bytes.Length is 0 or > (int)FederationEnrollmentRoutes.MaxRequestBodySize)
        {
            return null;
        }

        try
        {
            var authority = X509CertificateLoader.LoadCertificate(bytes);
            if (FederationCertificateAuthority.Fingerprint(authority) == authorityFingerprint)
            {
                return authority;
            }

            authority.Dispose();
        }
        catch (CryptographicException)
        {
            // Not a certificate, so not the authority.
        }

        return null;
    }

    /// <inheritdoc/>
    public Task<FederationIssuance> Redeem(Uri endpoint, X509Certificate2 authority, FederationRedeemRequest request, CancellationToken cancellationToken) =>
        Post(endpoint, FederationEnrollmentRoutes.Redeem, authority, null, request, cancellationToken);

    /// <inheritdoc/>
    public Task<FederationIssuance> Renew(Uri endpoint, X509Certificate2 authority, X509Certificate2 clientCertificate, FederationRenewRequest request, CancellationToken cancellationToken) =>
        Post(endpoint, FederationEnrollmentRoutes.Renew, authority, clientCertificate, request, cancellationToken);

    static SocketsHttpHandler CreateHandler(System.Net.Security.RemoteCertificateValidationCallback validate, X509Certificate2? clientCertificate)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = _timeout,
            AllowAutoRedirect = false,
            UseProxy = false
        };
#pragma warning disable CA5398 // Federation deliberately accepts TLS 1.3 only (decision 0027), never what the OS happens to choose.
        handler.SslOptions.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls13;
#pragma warning restore CA5398
        handler.SslOptions.RemoteCertificateValidationCallback = validate;
        if (clientCertificate is not null)
        {
            handler.SslOptions.ClientCertificates = [clientCertificate];
        }

        return handler;
    }

    async Task<FederationIssuance> Post<TRequest>(Uri endpoint, string route, X509Certificate2 authority, X509Certificate2? clientCertificate, TRequest request, CancellationToken cancellationToken)
    {
        using var handler = CreateHandler(
            (_, certificate, _, errors) => FederationServerChainValidator.Validate(certificate as X509Certificate2, errors, authority, timeProvider.GetUtcNow()),
            clientCertificate);
        using var client = new HttpClient(handler, false) { Timeout = _timeout };
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, route))
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = JsonContent.Create(request)
        };

        using var response = await client.SendAsync(message, cancellationToken);
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest)
        {
            var issuance = await response.Content.ReadFromJsonAsync<FederationIssuance>(cancellationToken);
            if (issuance is not null)
            {
                return issuance;
            }
        }

        return FederationIssuance.Rejected;
    }
}
