// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA5398 // Federation links are TLS 1.3 only by policy; the protocol is hardcoded on purpose.

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Federation;
using Cratis.DependencyInjection;
using Grpc.Net.Client;
using ProtoBuf.Grpc.Client;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Creates TLS 1.3 gRPC connections that present a named client certificate and trust a server only by its pinned public key.
/// </summary>
/// <param name="certificates">The client certificate provider.</param>
[Singleton]
public class FederationPullClientFactory(IFederationClientCertificates certificates) : IFederationPullClientFactory
{
    const int MaxMessageSize = 64 * 1024 * 1024;

    /// <inheritdoc/>
    public IFederationPullConnection Create(FederationBindingState binding)
    {
        if (!Uri.TryCreate(binding.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("A federation endpoint must be an absolute https address.");
        }

        var certificate = certificates.Get(binding.ClientCertificate);
        var handler = CreateHandler(certificate, binding.ServerPin);
        try
        {
            var channel = GrpcChannel.ForAddress(
                endpoint,
                new GrpcChannelOptions
                {
                    HttpHandler = handler,
                    DisposeHttpClient = true,
                    MaxReceiveMessageSize = MaxMessageSize,
                    MaxSendMessageSize = MaxMessageSize
                });

            return new Connection(
                channel,
                channel.CreateCallInvoker().CreateGrpcService<IFederationPull>(new InProcessAwareGrpcClientProxiesClientFactory()));
        }
        catch
        {
            handler.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Create the HTTP handler: TLS 1.3 only, the client certificate presented, the server validated by pin alone.
    /// </summary>
    /// <param name="clientCertificate">The client certificate to present.</param>
    /// <param name="serverPin">The pin the server certificate must match.</param>
    /// <returns>The handler.</returns>
    public SocketsHttpHandler CreateHandler(X509Certificate2 clientCertificate, string serverPin)
    {
        var handler = new SocketsHttpHandler();
        try
        {
            handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls13;
            handler.SslOptions.ClientCertificates = [clientCertificate];
            handler.SslOptions.RemoteCertificateValidationCallback = FederationPinValidator.CreateCallback(serverPin);
            return handler;
        }
        catch
        {
            handler.Dispose();
            throw;
        }
    }

    sealed class Connection(GrpcChannel channel, IFederationPull service) : IFederationPullConnection
    {
        public IFederationPull Service { get; } = service;

        public void Dispose() => channel.Dispose();
    }
}
