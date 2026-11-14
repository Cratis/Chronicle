// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA5398 // Federation links are TLS 1.3 only by policy; the protocol is hardcoded on purpose.

using System.Security.Authentication;
using Cratis.DependencyInjection;
using Grpc.Net.Client;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationDialClientFactory"/>: TLS 1.3 only, the enrolled certificate presented, and the
/// server accepted only when it chains to the authority root that came with the enrollment, so the system trust store is never consulted.
/// </summary>
/// <param name="identities">The <see cref="IFederationEnrolledIdentities"/> holding the certificate obtained by enrollment.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
[Singleton]
public class FederationDialClientFactory(IFederationEnrolledIdentities identities, TimeProvider timeProvider) : IFederationDialClientFactory
{
    const int MaxMessageSize = 64 * 1024 * 1024;

    /// <inheritdoc/>
    public async Task<string?> GetCurrentSerial() => (await identities.Get())?.Serial;

    /// <inheritdoc/>
    public async Task<IFederationPullConnection> Create(FederationBindingState binding)
    {
        var identity = await identities.Get() ?? throw new InvalidOperationException("This kernel is not enrolled with a federation authority.");

        var address = string.IsNullOrEmpty(binding.Endpoint) ? identity.Endpoint : binding.Endpoint;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("A federation endpoint must be an absolute https address.");
        }

        var handler = new SocketsHttpHandler
        {
            // One long-lived connection carries a stream in each direction, so a dead peer has to be noticed.
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(20),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always
        };
        try
        {
            handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls13;
            handler.SslOptions.ClientCertificates = [identity.Certificate];
            var authority = identity.Authority;
            handler.SslOptions.RemoteCertificateValidationCallback =
                (_, certificate, _, errors) => FederationServerChainValidator.Validate(certificate as System.Security.Cryptography.X509Certificates.X509Certificate2, errors, authority, timeProvider.GetUtcNow());

            var channel = GrpcChannel.ForAddress(
                endpoint,
                new GrpcChannelOptions
                {
                    HttpHandler = handler,
                    DisposeHttpClient = true,
                    MaxReceiveMessageSize = MaxMessageSize,
                    MaxSendMessageSize = MaxMessageSize
                });
            return FederationPullClientFactory.CreateConnection(channel);
        }
        catch
        {
            handler.Dispose();
            throw;
        }
    }
}
