// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationEnrollmentTransport"/> over HTTP/2 and TLS 1.3.
/// </summary>
/// <remarks>
/// The endpoint comes from an enrollment code, so it is checked by the <see cref="FederationEndpointPolicy"/> and the connection is made to
/// the address that was checked. There are no redirects and no proxy. Reaching the authority and redeeming a code fail the same way for
/// every cause, after the same time, so that this kernel is not an oracle for what an address can reach.
/// </remarks>
/// <param name="timeProvider">The <see cref="TimeProvider"/> to check certificate validity with.</param>
/// <param name="policy">The <see cref="FederationEndpointPolicy"/> deciding which endpoints may be connected to.</param>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the enrollment configuration.</param>
[Singleton]
public class FederationEnrollmentTransport(TimeProvider timeProvider, FederationEndpointPolicy policy, IOptions<ChronicleOptions> options) : IFederationEnrollmentTransport
{
    const int MaxAuthorityBytes = (int)FederationEnrollmentRoutes.MaxRequestBodySize;
    const int MaxIssuanceBytes = 64 * 1024;
    static readonly TimeSpan _renewalTimeout = TimeSpan.FromSeconds(30);

    TimeSpan FailureDuration => options.Value.Federation.Enrollment.FailureDuration;

    TimeSpan AttemptTimeout => FailureDuration > TimeSpan.Zero ? FailureDuration : _renewalTimeout;

    /// <inheritdoc/>
    public async Task<X509Certificate2?> GetAuthority(Uri endpoint, string authorityFingerprint, CancellationToken cancellationToken)
    {
        var started = timeProvider.GetTimestamp();
        X509Certificate2? authority = null;
        try
        {
            authority = await FetchAuthority(endpoint, authorityFingerprint, cancellationToken);
        }
        catch (Exception ex) when (IsConnectionFailure(ex, cancellationToken))
        {
            // Every way of not reaching the authority is the same answer.
        }

        if (authority is null)
        {
            await PadFailure(started, cancellationToken);
        }

        return authority;
    }

    /// <inheritdoc/>
    public async Task<FederationIssuance> Redeem(Uri endpoint, X509Certificate2 authority, FederationRedeemRequest request, CancellationToken cancellationToken)
    {
        var started = timeProvider.GetTimestamp();
        var issuance = FederationIssuance.Rejected;
        try
        {
            issuance = await Post(endpoint, FederationEnrollmentRoutes.Redeem, authority, null, request, AttemptTimeout, cancellationToken);
        }
        catch (Exception ex) when (IsConnectionFailure(ex, cancellationToken))
        {
            // Every way of not completing the redemption is the same answer.
        }

        if (issuance.Outcome != FederationIssuanceOutcome.Issued)
        {
            await PadFailure(started, cancellationToken);
        }

        return issuance;
    }

    /// <inheritdoc/>
    public Task<FederationIssuance> Renew(Uri endpoint, X509Certificate2 authority, X509Certificate2 clientCertificate, FederationRenewRequest request, CancellationToken cancellationToken) =>
        Post(endpoint, FederationEnrollmentRoutes.Renew, authority, clientCertificate, request, _renewalTimeout, cancellationToken);

    /// <summary>
    /// Reads a response body, and gives up the moment it is longer than allowed, so a hostile endpoint cannot stream an unbounded body
    /// for the length of the attempt. Whatever the reason, the caller treats null as the same uniform failure.
    /// </summary>
    /// <param name="content">The <see cref="HttpContent"/> of the response.</param>
    /// <param name="maxBytes">The most bytes to accept.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The body, or null when it is longer than allowed.</returns>
    internal static async Task<byte[]?> ReadLimited(HttpContent content, int maxBytes, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > maxBytes)
        {
            return null;
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        await using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        int read;
        while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > maxBytes)
            {
                return null;
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return buffer.ToArray();
    }

    static bool IsConnectionFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or SocketException or AuthenticationException or IOException or InvalidOperationException
        || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested);

    static SocketsHttpHandler CreateHandler(System.Net.Security.RemoteCertificateValidationCallback validate, X509Certificate2? clientCertificate, IPAddress[] addresses, TimeSpan timeout)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = timeout,
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = async (context, cancellationToken) =>
            {
                // Connect to the addresses that were checked, not to whatever the name resolves to now.
                SocketException? failure = null;
                foreach (var address in addresses)
                {
                    var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    try
                    {
                        await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                        return new NetworkStream(socket, true);
                    }
                    catch (SocketException ex)
                    {
                        socket.Dispose();
                        failure = ex;
                    }
                    catch
                    {
                        socket.Dispose();
                        throw;
                    }
                }

                throw failure ?? new SocketException((int)SocketError.HostNotFound);
            }
        };
#pragma warning disable CA5398 // Federation deliberately accepts TLS 1.3 only (decision 0027), never what the OS happens to choose.
        handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls13;
#pragma warning restore CA5398
        handler.SslOptions.RemoteCertificateValidationCallback = validate;
        if (clientCertificate is not null)
        {
            handler.SslOptions.ClientCertificates = [clientCertificate];
        }

        return handler;
    }

    async Task PadFailure(long started, CancellationToken cancellationToken)
    {
        var remaining = FailureDuration - timeProvider.GetElapsedTime(started);
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, timeProvider, cancellationToken);
        }
    }

    async Task<X509Certificate2?> FetchAuthority(Uri endpoint, string authorityFingerprint, CancellationToken cancellationToken)
    {
        var addresses = await policy.Resolve(endpoint, cancellationToken);
        if (addresses is null)
        {
            return null;
        }

        // The response is public data that is accepted only if it matches the fingerprint in the code, so the server is not validated here.
#pragma warning disable CA5359 // See above: the response is checked against the fingerprint instead.
        using var handler = CreateHandler((_, _, _, _) => true, null, addresses, AttemptTimeout);
#pragma warning restore CA5359
        using var client = new HttpClient(handler, false) { Timeout = AttemptTimeout, MaxResponseContentBufferSize = MaxAuthorityBytes };
        using var message = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint, FederationEnrollmentRoutes.Authority))
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var bytes = await ReadLimited(response.Content, MaxAuthorityBytes, cancellationToken);
        if (bytes is null || bytes.Length == 0)
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

    async Task<FederationIssuance> Post<TRequest>(Uri endpoint, string route, X509Certificate2 authority, X509Certificate2? clientCertificate, TRequest request, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var addresses = await policy.Resolve(endpoint, cancellationToken);
        if (addresses is null)
        {
            return FederationIssuance.Rejected;
        }

        using var handler = CreateHandler(
            (_, certificate, _, errors) => FederationServerChainValidator.Validate(certificate as X509Certificate2, errors, authority, timeProvider.GetUtcNow()),
            clientCertificate,
            addresses,
            timeout);
        using var client = new HttpClient(handler, false) { Timeout = timeout, MaxResponseContentBufferSize = MaxIssuanceBytes };
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(endpoint, route))
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = JsonContent.Create(request)
        };

        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Forbidden or HttpStatusCode.BadRequest)
        {
            var bytes = await ReadLimited(response.Content, MaxIssuanceBytes, cancellationToken);
            if (bytes is not null)
            {
                try
                {
                    var issuance = JsonSerializer.Deserialize<FederationIssuance>(bytes, JsonSerializerOptions.Web);
                    if (issuance is not null)
                    {
                        return issuance;
                    }
                }
                catch (JsonException)
                {
                    // Not an answer of the authority.
                }
            }
        }

        return FederationIssuance.Rejected;
    }
}
