// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
namespace Cratis.Chronicle.Server.for_ReloadingCertificate.given;

/// <summary>
/// A reloading certificate whose keystore is laid out the way Kubernetes mounts a Secret: a directory per version,
/// a ..data directory link pointing at the current version, and the keystore a link through ..data.
/// </summary>
public class a_kubernetes_secret_volume : Specification
{
    protected string _volume;
    protected string _keystore;
    protected ManualTime _time;
    protected X509Certificate2 _first;
    protected X509Certificate2 _second;
    internal ReloadingCertificate _certificate;
    protected int _reloads;
    protected int _failures;
    int _version;

    void Establish()
    {
        _volume = Path.Combine(Path.GetTempPath(), $"chronicle-secret-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_volume);
        _keystore = Path.Combine(_volume, "keystore.p12");
        _time = new ManualTime();
        _first = TestCertificates.Create("CN=first");
        _second = CreateRenewal();

        if (!SymbolicLinkFactAttribute.Supported)
        {
            return;
        }

        Publish(_first);
        File.CreateSymbolicLink(_keystore, Path.Combine("..data", "keystore.p12"));
        _certificate = new ReloadingCertificate(
            new Tls { Enabled = true, CertificatePath = _keystore, CertificatePassword = TestCertificates.Password },
            _first,
            TimeSpan.FromSeconds(30),
            _time,
            _ => _reloads++,
            _ => _failures++);
    }

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
        if (Directory.Exists(_volume))
        {
            Directory.Delete(_volume, true);
        }
    }

    /// <summary>
    /// Creates the certificate a renewal replaces the first one with; override to change its shape.
    /// </summary>
    /// <returns>The renewed certificate.</returns>
    protected virtual X509Certificate2 CreateRenewal() => TestCertificates.Create("CN=second");

    /// <summary>
    /// Publishes a certificate the way the kubelet does: write a new version directory, then repoint ..data at it.
    /// The keystore.p12 link itself is never touched.
    /// </summary>
    /// <param name="certificate">The certificate to publish.</param>
    protected void Publish(X509Certificate2 certificate)
    {
        var versionName = $"..2024_01_01_00_00_0{_version++}";
        var versionDirectory = Path.Combine(_volume, versionName);
        Directory.CreateDirectory(versionDirectory);
        File.WriteAllBytes(Path.Combine(versionDirectory, "keystore.p12"), TestCertificates.Export(certificate));

        var data = Path.Combine(_volume, "..data");
        if (Directory.Exists(data))
        {
            Directory.Delete(data);
        }

        Directory.CreateSymbolicLink(data, versionName);
    }
}
