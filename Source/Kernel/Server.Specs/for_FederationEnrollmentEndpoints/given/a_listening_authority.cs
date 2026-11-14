// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Federation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.for_FederationEnrollmentEndpoints.given;

/// <summary>
/// A real Kestrel federation listener, TLS 1.3 over HTTP/2, that presents a certificate issued by its own authority and serves
/// the enrollment routes, with a grain that issues for one enrollment.
/// </summary>
public class a_listening_authority : Specification
{
    protected X509Certificate2 _authority;
    protected string _fingerprint;
    protected Uri _endpoint;
    protected IFederationAuthority _grain;
    protected ChronicleOptions _options;
    WebApplication _app;

    protected virtual int RequestsPerMinute => 1000;

    async Task Establish()
    {
        if (!Tls13.Supported)
        {
            return;
        }

        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        _endpoint = new($"https://localhost:{port}/");
        _authority = FederationCertificateAuthority.CreateAuthority("example", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        _fingerprint = FederationCertificateAuthority.Fingerprint(_authority);
        _options = new()
        {
            Federation = new()
            {
                Enabled = true,
                Port = port,
                Hosted = new() { Enabled = true, PublicEndpoint = _endpoint.ToString(), TrustDomain = "example", RequestsPerMinutePerSource = RequestsPerMinute }
            }
        };

        var keys = Substitute.For<IFederationAuthorityKeys>();
        keys.GetOrCreate().Returns((_authority, false));

        _grain = Substitute.For<IFederationAuthority>();
        _grain.EnsureAuthority().Returns(_fingerprint);
        _grain.Redeem("known", "secret", Arg.Any<byte[]>()).Returns(call => Task.FromResult(IssueFor(call.ArgAt<byte[]>(2))));
        _grain.Redeem(Arg.Is<string>(_ => _ != "known"), Arg.Any<string>(), Arg.Any<byte[]>()).Returns(FederationIssuance.Rejected);

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IFederationAuthority>(IFederationAuthority.Key, null).Returns(_grain);

        var listenerCertificate = new FederationListenerCertificate(keys, Options.Create(_options), TimeProvider.System);
        await listenerCertificate.Refresh();

        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddSingleton(Options.Create(_options));
        builder.Services.AddSingleton(grainFactory);
        builder.Services.AddSingleton(keys);
        builder.Services.AddSingleton(new FederationEnrollmentRateLimiter(TimeProvider.System));
        builder.WebHost.UseKestrel(kestrel =>
            kestrel.Listen(IPAddress.Loopback, port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
                listenOptions.UseHttps(new TlsHandshakeCallbackOptions
                {
                    OnConnection = _ => ValueTask.FromResult(new System.Net.Security.SslServerAuthenticationOptions
                    {
                        ServerCertificateContext = listenerCertificate.Current,
#pragma warning disable CA5398 // Federation deliberately accepts TLS 1.3 only.
                        EnabledSslProtocols = SslProtocols.Tls13,
#pragma warning restore CA5398
                        ClientCertificateRequired = true,
#pragma warning disable CA5359 // As in the kernel: identity is decided per route.
                        RemoteCertificateValidationCallback = static (_, _, _, _) => true,
#pragma warning restore CA5359
                        ApplicationProtocols = [System.Net.Security.SslApplicationProtocol.Http2]
                    })
                });
            }));
        _app = builder.Build();
        _app.MapFederationEnrollment();
        await _app.StartAsync();
    }

    async Task Destroy()
    {
        if (_app is null)
        {
            return;
        }

        await _app.StopAsync();
        await _app.DisposeAsync();
        _authority.Dispose();
    }

    protected static byte[] NewCsr()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return new CertificateRequest("CN=peer", key, HashAlgorithmName.SHA256).CreateSigningRequest();
    }

    FederationIssuance IssueFor(byte[] csr)
    {
        using var certificate = FederationCertificateAuthority.IssueClient(_authority, csr, "spiffe://example/customer/acme/kernel/prod", DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(14));
        return new(
            FederationIssuanceOutcome.Issued,
            Convert.ToBase64String(certificate.RawData),
            Convert.ToBase64String(_authority.RawData),
            "spiffe://example/customer/acme/kernel/prod",
            certificate.SerialNumber,
            new DateTimeOffset(certificate.NotAfter),
            _endpoint.ToString());
    }
}
