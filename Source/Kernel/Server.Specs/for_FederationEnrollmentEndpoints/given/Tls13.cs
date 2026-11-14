// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server.for_FederationEnrollmentEndpoints.given;

/// <summary>
/// Tells whether this machine can negotiate TLS 1.3, which federation requires. Some platforms cannot (for instance macOS),
/// and a specification that needs it is reported as skipped there rather than as a failure.
/// </summary>
public static class Tls13
{
    static readonly Lazy<bool> _supported = new(Probe);

    /// <summary>
    /// Gets a value indicating whether TLS 1.3 can be negotiated on this machine.
    /// </summary>
    public static bool Supported => _supported.Value;

    /// <summary>
    /// Gets the reason a TLS 1.3 specification is skipped, or null when it runs.
    /// </summary>
    public static string? SkipReason => Supported ? null : "This platform cannot negotiate TLS 1.3, which federation requires.";

    static bool Probe()
    {
        try
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            using var created = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256).CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            using var certificate = X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pfx), null);

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var port = ((IPEndPoint)listener.LocalEndpoint).Port;
                var accept = Task.Run(async () =>
                {
                    using var server = await listener.AcceptTcpClientAsync();
                    await using var tls = new SslStream(server.GetStream());
#pragma warning disable CA5398 // The probe asks for exactly TLS 1.3.
                    await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = certificate, EnabledSslProtocols = SslProtocols.Tls13 });
#pragma warning restore CA5398
                });

                using var client = new TcpClient();
                client.Connect(IPAddress.Loopback, port);
                using var clientTls = new SslStream(client.GetStream());
#pragma warning disable CA5398, CA5359 // The probe asks for exactly TLS 1.3 and trusts its own certificate.
                clientTls.AuthenticateAsClient(new SslClientAuthenticationOptions { TargetHost = "localhost", EnabledSslProtocols = SslProtocols.Tls13, RemoteCertificateValidationCallback = (_, _, _, _) => true });
#pragma warning restore CA5398, CA5359
                accept.Wait(TimeSpan.FromSeconds(5));
#pragma warning disable CA5398 // Comparing against the one protocol federation requires.
                return clientTls.SslProtocol == SslProtocols.Tls13;
#pragma warning restore CA5398
            }
            finally
            {
                listener.Stop();
            }
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException or AuthenticationException or AggregateException or IOException or SocketException)
        {
            return false;
        }
    }
}
