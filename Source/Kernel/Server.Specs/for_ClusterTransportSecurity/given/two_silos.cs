// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Server.for_ClusterTransportSecurity.given;

/// <summary>
/// Two real Orleans silos on free loopback ports whose transport is secured the way the kernel secures it.
/// </summary>
public class two_silos : Specification
{
    protected X509Certificate2 _firstCertificate;
    protected IHost _first;
    protected IHost _second;
    protected int _primaryPort;
    protected int _secondPort;
    protected int _secondGatewayPort;

    protected void Establish()
    {
        _firstCertificate = SelfSigned("CN=chronicle");
        _primaryPort = FreePort();
        _secondPort = FreePort();
        _secondGatewayPort = FreePort();
        _first = CreateSilo(_primaryPort, FreePort(), _firstCertificate);
    }

    protected async Task Destroy()
    {
        await StopQuietly(_second);
        await StopQuietly(_first);
        _second?.Dispose();
        _first.Dispose();
        _firstCertificate.Dispose();
    }

    protected IHost CreateSilo(int siloPort, int gatewayPort, X509Certificate2 certificate)
    {
        var tls = new Tls { CertificatePath = Path.Combine(Path.GetTempPath(), $"chronicle-silo-{Guid.NewGuid():N}.pfx") };
        var reloading = new ReloadingCertificate(tls, certificate, TimeSpan.FromSeconds(30), TimeProvider.System);
        return Host.CreateDefaultBuilder()
            .ConfigureLogging(logging => logging.ClearProviders())
            .UseOrleans(silo =>
            {
                silo.UseLocalhostClustering(siloPort, gatewayPort, new IPEndPoint(IPAddress.Loopback, _primaryPort));
                silo.UseTls(options => ClusterTransportSecurity.Apply(options, tls, reloading));
            })
            .Build();
    }

    protected async Task<int> WaitForActiveSilos(int expected, TimeSpan timeout)
    {
        var membership = _first.Services.GetRequiredService<IClusterMembershipService>();
        var deadline = DateTime.UtcNow + timeout;
        var active = 0;
        while (DateTime.UtcNow < deadline)
        {
            active = membership.CurrentSnapshot.Members.Values.Count(member => member.Status == SiloStatus.Active);
            if (active >= expected)
            {
                break;
            }

            await Task.Delay(100);
        }

        return active;
    }

    protected static X509Certificate2 SelfSigned(string subject)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pkcs12, "pw"), "pw", X509KeyStorageFlags.Exportable);
    }

    static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    static async Task StopQuietly(IHost? host)
    {
        if (host is null)
        {
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await host.StopAsync(cts.Token);
        }
        catch (Exception ex) when (ex is OperationCanceledException or InvalidOperationException)
        {
            // A silo that never joined has nothing to shut down gracefully.
        }
    }
}
